using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GeneralTestApi.Base;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GeneralTestApi;

public sealed class EventBusRabbitMq : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly RabbitMqEventBusConfig _rabbitMqEventBusConfig;
    private readonly RabbitMqConnectionSettings _rabbitMqConnectionSettings;

    private readonly IEventBusSubscriptionsManager _subsManager;

    [CanBeNull]
    private readonly IModel _consumerChannel;

    private readonly SemaphoreSlim semaphore;

    private bool _disposed;
    private bool _publishing;
    private bool _consuming;

    public EventBusRabbitMq(IRabbitMqPersistentConnection persistentConnection,
        IOptions<RabbitMqEventBusConfig> rabbitMqEventBusConfig,
        IOptions<RabbitMqConnectionSettings> rabbitMqConnectionSettings, IServiceProvider serviceProvider)
    {
        _persistentConnection = persistentConnection;
        _serviceProvider = serviceProvider;
        _rabbitMqEventBusConfig = rabbitMqEventBusConfig.Value;
        _rabbitMqConnectionSettings = rabbitMqConnectionSettings.Value;

        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);

        _consumerChannel = CreateConsumerChannel();
        semaphore = new SemaphoreSlim(_rabbitMqEventBusConfig.ConsumerMaxThreadCount);
    }

    public async Task PublishAsync<TEventMessage>(TEventMessage eventMessage, ParentMessageEnvelope parentMessage = null) where TEventMessage : IIntegrationEventMessage
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        _publishing = true;

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_rabbitMqConnectionSettings.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _publishing = false;
                Console.WriteLine("RabbitMQ | Could not publish event message : {0} after {1}s ({2})", eventMessage, $"{time.TotalSeconds:n1}", ex.Message);
            });

        var eventName = eventMessage.GetType().Name;
        eventName = TrimEventName(eventName);

        var @event = new MessageEnvelope<TEventMessage>
        {
            ParentMessageId = parentMessage?.MessageId,
            MessageId = Guid.NewGuid(),
            MessageTime = DateTime.UtcNow,
            Message = eventMessage,
            Producer = _rabbitMqEventBusConfig.ClientInfo,
            CorrelationId = parentMessage?.CorrelationId,
            Channel = parentMessage?.Channel,
            UserId = parentMessage?.UserId,
            UserRoleUniqueName = parentMessage?.UserRoleUniqueName,
            HopLevel = parentMessage != null ? parentMessage.HopLevel + 1 : 1
        };

        Console.WriteLine("RabbitMQ | {0} PRODUCER [ {1} ] => MessageId [ {2} ] STARTED", _rabbitMqEventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

        Console.WriteLine("RabbitMQ | Creating channel to publish event name: {0}", eventName);
        policy.Execute(() =>
        {
            using var publisherChannel = _persistentConnection.CreateModel();

            Console.WriteLine("RabbitMQ | Declaring exchange to publish event name: {0}", eventName);
            publisherChannel.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct"); //Ensure exchange exists while publishing

            var properties = publisherChannel?.CreateBasicProperties();
            properties!.DeliveryMode = 2; // persistent

            Console.WriteLine("RabbitMQ | Publishing event: {0}", @event);
            publisherChannel.BasicPublish(
                exchange: _rabbitMqEventBusConfig.ExchangeName,
                routingKey: eventName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        });

        Console.WriteLine("RabbitMQ | {0} PRODUCER [ {1} ] => MessageId [ {2} ] COMPLETED", _rabbitMqEventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());
        _publishing = false;
    }

    public void Subscribe<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>
    {
        Subscribe(typeof(T), typeof(TH));
    }

    public void Subscribe(Type eventType, Type eventHandlerType)
    {
        if (!eventType.IsAssignableTo(typeof(IIntegrationEventMessage))) throw new TypeAccessException();
        if (!eventHandlerType.IsAssignableTo(typeof(IIntegrationEventHandler))) throw new TypeAccessException();

        var eventName = eventType.Name;
        eventName = TrimEventName(eventName);

        if (!_subsManager.HasSubscriptionsForEvent(eventName))
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            _consumerChannel?.QueueDeclare(queue: GetConsumerQueueName(eventName), //Ensure queue exists while consuming
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // take 1 message per consumer
            _consumerChannel?.BasicQos(0, _rabbitMqEventBusConfig.ConsumerMaxThreadCount, false);

            _consumerChannel?.QueueBind(queue: GetConsumerQueueName(eventName),
                exchange: _rabbitMqEventBusConfig.ExchangeName,
                routingKey: eventName);
        }

        Console.WriteLine("RabbitMQ | Subscribing to event {0} with {1}", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType);
        StartBasicConsume(eventName);
    }

    private void StartBasicConsume(string eventName)
    {
        Console.WriteLine("RabbitMQ | {0} CONSUMER [ {1} ] => Subscribed", _rabbitMqEventBusConfig.ClientInfo, eventName);

        if (_consumerChannel != null)
        {
            var consumer = new EventingBasicConsumer(_consumerChannel);
            consumer.Received += ConsumerReceived;

            _consumerChannel.BasicConsume(
                queue: GetConsumerQueueName(eventName),
                autoAck: false,
                consumer: consumer);
        }
        else
        {
            Console.WriteLine("RabbitMQ | StartBasicConsume can't call on _consumerChannel == null");
        }
    }

    private void ConsumerReceived([CanBeNull] object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_disposed) return;
        _consuming = true;

        var eventName = eventArgs.RoutingKey;
        eventName = TrimEventName(eventName);

        Console.WriteLine("RabbitMQ | {0} CONSUMER [ {1} ] => Consume STARTED", _rabbitMqEventBusConfig.ClientInfo, eventName);

        semaphore.Wait();
        var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        Task.Run(() =>
        {
            try
            {
                Console.WriteLine("RabbitMQ | {0} CONSUMER [ {1} ] => Received: {2}", _rabbitMqEventBusConfig.ClientInfo, eventName, message);
                ProcessEvent(eventName, message);

                // Even on exception we take the message off the queue.
                // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX).
                // For more information see: https://www.rabbitmq.com/dlx.html
                _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                Console.WriteLine("RabbitMQ | {0} CONSUMER [ {1} ] => Consume COMPLETED", _rabbitMqEventBusConfig.ClientInfo, eventName);
            }
            catch (TimeoutException timeProblem)
            {
                // re-queue 
                _consumerChannel?.BasicNack(eventArgs.DeliveryTag, false, true);
                //channel.BasicReject(e.DeliveryTag, true);

                Console.WriteLine("RabbitMQ | {0} CONSUMER [ {1} ] => Consume TIMEOUT RETRY", _rabbitMqEventBusConfig.ClientInfo, eventName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("RabbitMQ | {0} CONSUMER [ {1} ] => Consume ERROR : {2} | {3}", _rabbitMqEventBusConfig.ClientInfo, eventName, ex.Message, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));

                // TODO: Publish error queue
                // var eventType = _subsManager.GetEventTypeByName($"{_rabbitMqEventBusConfig.EventNamePrefix}{eventName}{_rabbitMqEventBusConfig.EventNameSuffix}");
                //
                // var genericClass = typeof(MessageEnvelope<>);
                // var constructedClass = genericClass.MakeGenericType(eventType!);
                // var @event = JsonConvert.DeserializeObject(message, constructedClass);
                
                // YENI KUYRUK ICIN EXCHANGE "" olacak, direkt HATA KURUGUNA ATILACAK HHS_IdentityService_Error

                // remove from old queue 
                _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            finally
            {
                semaphore.Release();
            }
        });

        _consuming = false;
    }

    private void ProcessEvent(string eventName, string message)
    {
        eventName = TrimEventName(eventName);

        if (_subsManager.HasSubscriptionsForEvent(eventName))
        {
            var subscriptions = _subsManager.GetHandlersForEvent(eventName);
            foreach (var subscription in subscriptions)
            {
                var eventType = _subsManager.GetEventTypeByName($"{_rabbitMqEventBusConfig.EventNamePrefix}{eventName}{_rabbitMqEventBusConfig.EventNameSuffix}");

                var genericClass = typeof(MessageEnvelope<>);
                var constructedClass = genericClass.MakeGenericType(eventType!);
                var @event = JsonConvert.DeserializeObject(message, constructedClass);

                var handler = _serviceProvider.GetService(subscription.HandlerType);
                if (handler == null)
                {
                    Console.WriteLine("RabbitMQ | {0} CONSUMER [ {1} ] => No HANDLER for event", _rabbitMqEventBusConfig.ClientInfo, eventName);
                    continue;
                }

                try
                {
                    Console.WriteLine("RabbitMQ | {0} CONSUMER [ {1} ] => Handling STARTED : Event [ {2} ]", _rabbitMqEventBusConfig.ClientInfo, eventName, @event);
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType!);
                    (((Task)concreteType.GetMethod("HandleAsync")?.Invoke(handler, new[] { @event }))!).GetAwaiter().GetResult();
                    Console.WriteLine("RabbitMQ | {0} CONSUMER [ {1} ] => Handling COMPLETED : Event [ {2} ]", _rabbitMqEventBusConfig.ClientInfo, eventName, @event);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("RabbitMQ | {0} CONSUMER [ {1} ] => Handling ERROR : {2}", _rabbitMqEventBusConfig.ClientInfo, eventName, ex.Message);
                    throw new Exception("Message Process Error");
                }
            }
        }
        else
        {
            Console.WriteLine("RabbitMQ | {0} CONSUMER [ {1} ] => No SUBSCRIPTION for event", _rabbitMqEventBusConfig.ClientInfo, eventName);
        }
    }

    private string GetConsumerQueueName(string eventName)
    {
        return $"{_rabbitMqEventBusConfig.ClientInfo}_{TrimEventName(eventName)}";
    }

    private string TrimEventName(string eventName)
    {
        if (_rabbitMqEventBusConfig.DeleteEventPrefix && eventName.StartsWith(_rabbitMqEventBusConfig.EventNamePrefix))
        {
            eventName = eventName.Substring(_rabbitMqEventBusConfig.EventNamePrefix.Length);
        }

        if (_rabbitMqEventBusConfig.DeleteEventSuffix && eventName.EndsWith(_rabbitMqEventBusConfig.EventNameSuffix))
        {
            eventName = eventName.Substring(0, eventName.Length - _rabbitMqEventBusConfig.EventNameSuffix.Length);
        }

        return eventName;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        Thread.Sleep(1000); //wait for dispose set

        while (_publishing || _consuming || semaphore.CurrentCount < _rabbitMqEventBusConfig.ConsumerMaxThreadCount)
        {
            Console.WriteLine("Publisher and Consumers are waiting...");
            Thread.Sleep(1000);
        }

        semaphore.Dispose();
        _consumerChannel?.Dispose();
        _subsManager.Clear();
    }

    [CanBeNull]
    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var channel = _persistentConnection.CreateModel();

        channel?.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct");

        return channel;
    }
}