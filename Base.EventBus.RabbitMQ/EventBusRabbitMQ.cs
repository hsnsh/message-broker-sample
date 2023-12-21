using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Base.EventBus.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Base.EventBus.RabbitMQ;

// ReSharper disable once InconsistentNaming
public class EventBusRabbitMQ : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQPersistentConnection _persistentConnection;
    private readonly EventBusConfig _eventBusConfig;
    private readonly ILogger<EventBusRabbitMQ> _logger;
    //private readonly IHttpContextAccessor _contextAccessor;
    
    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly IModel? _consumerChannel;

    public EventBusRabbitMQ(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _eventBusConfig = _serviceProvider.GetRequiredService<IOptions<EventBusConfig>>().Value;
        _persistentConnection = _serviceProvider.GetRequiredService<IRabbitMQPersistentConnection>();

        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<EventBusRabbitMQ>() ?? throw new ArgumentNullException(nameof(loggerFactory));

        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);

        _consumerChannel = CreateConsumerChannel();
        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    public Task PublishAsync<TEventMessage>(TEventMessage eventMessage, Guid? relatedMessageId = null) where TEventMessage : IIntegrationEventMessage
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_eventBusConfig.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning(ex, "RabbitMQ | Could not publish event message : {Event} after {Timeout}s ({ExceptionMessage})", eventMessage, $"{time.TotalSeconds:n1}", ex.Message);
            });

        var eventName = eventMessage.GetType().Name;
        eventName = TrimEventName(eventName);

        _logger.LogDebug("RabbitMQ | Creating channel to publish event name: {EventName}", eventName);

        using (var channel = _persistentConnection.CreateModel())
        {
            _logger.LogDebug("RabbitMQ | Declaring exchange to publish event name: {EventName}", eventName);

            channel.ExchangeDeclare(exchange: _eventBusConfig.ExchangeName, type: "direct"); //Ensure exchange exists while publishing
            
            //var correlationId = _contextAccessor.HttpContext?.GetCorrelationId() ?? Guid.NewGuid().ToString("N");
            var @event = new MessageEnvelope<TEventMessage>
            {
                RelatedMessageId = relatedMessageId,
                MessageId = Guid.NewGuid(),
                MessageTime = DateTime.UtcNow,
                Message = eventMessage,
                Producer = _eventBusConfig.ClientInfo,
                //CorrelationId = correlationId,
            };
            
            _logger.LogInformation("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] STARTED", _eventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());

            var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

            policy.Execute(() =>
            {
                var properties = channel?.CreateBasicProperties();
                properties!.DeliveryMode = 2; // persistent

                _logger.LogDebug("RabbitMQ | Publishing event: {Event}", @event);

                channel?.BasicPublish(
                    exchange: _eventBusConfig.ExchangeName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });

            _logger.LogInformation("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] COMPLETED", _eventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());
        }

        return Task.CompletedTask;
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

            _consumerChannel?.QueueBind(queue: GetConsumerQueueName(eventName),
                exchange: _eventBusConfig.ExchangeName,
                routingKey: eventName);
        }

        _logger.LogInformation("RabbitMQ | Subscribing to event {EventName} with {EventHandler}", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType);
        StartBasicConsume(eventName);
    }

    public void Unsubscribe<T, TH>() where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();
        eventName = TrimEventName(eventName);

        _logger.LogInformation("RabbitMQ | Unsubscribing from event {EventName}", eventName);

        _subsManager.RemoveSubscription<T, TH>();
    }

    public void Dispose()
    {
        if (_consumerChannel != null)
        {
            _consumerChannel.Dispose();
        }

        _subsManager.Clear();
    }

    private void SubsManager_OnEventRemoved(object? sender, string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        using (var channel = _persistentConnection.CreateModel())
        {
            channel.QueueUnbind(queue: GetConsumerQueueName(eventName),
                exchange: _eventBusConfig.ExchangeName,
                routingKey: TrimEventName(eventName));

            if (_subsManager.IsEmpty)
            {
                _consumerChannel?.Close();
            }
        }
    }

    private IModel? CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var channel = _persistentConnection.CreateModel();

        channel?.ExchangeDeclare(exchange: _eventBusConfig.ExchangeName, type: "direct");

        return channel;
    }

    private void StartBasicConsume(string eventName)
    {
        _logger.LogInformation("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Subscribed", _eventBusConfig.ClientInfo, eventName);

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
            _logger.LogError("RabbitMQ | StartBasicConsume can't call on _consumerChannel == null");
        }
    }

    private async void ConsumerReceived(object? sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;
        eventName = TrimEventName(eventName);

        _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume STARTED", _eventBusConfig.ClientInfo, eventName);
        try
        {
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Received: {Message}", _eventBusConfig.ClientInfo, eventName, message);
            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume ERROR : {ConsumeError} | {Time}", _eventBusConfig.ClientInfo, eventName, ex.Message, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));
        }

        // Even on exception we take the message off the queue.
        // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
        // For more information see: https://www.rabbitmq.com/dlx.html
        _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
        _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume COMPLETED", _eventBusConfig.ClientInfo, eventName);
    }

    private string GetConsumerQueueName(string eventName)
    {
        return $"{_eventBusConfig.ClientInfo}_{TrimEventName(eventName)}";
    }

    private string TrimEventName(string eventName)
    {
        if (_eventBusConfig.DeleteEventPrefix && eventName.StartsWith(_eventBusConfig.EventNamePrefix))
        {
            eventName = eventName[_eventBusConfig.EventNamePrefix.Length..];
        }

        if (_eventBusConfig.DeleteEventSuffix && eventName.EndsWith(_eventBusConfig.EventNameSuffix))
        {
            eventName = eventName[..^_eventBusConfig.EventNameSuffix.Length];
        }

        return eventName;
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        eventName = TrimEventName(eventName);

        if (_subsManager.HasSubscriptionsForEvent(eventName))
        {
            var subscriptions = _subsManager.GetHandlersForEvent(eventName);

            using (var scope = _serviceProvider.CreateScope())
            {
                foreach (var subscription in subscriptions)
                {
                    var handler = _serviceProvider.GetService(subscription.HandlerType);
                    if (handler == null)
                    {
                        _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => No HANDLER for event", _eventBusConfig.ClientInfo, eventName);
                        continue;
                    }

                    try
                    {
                        var eventType = _subsManager.GetEventTypeByName($"{_eventBusConfig.EventNamePrefix}{eventName}{_eventBusConfig.EventNameSuffix}");
                      
                        Type genericClass = typeof(MessageEnvelope<>);
                        Type constructedClass = genericClass.MakeGenericType(eventType);
                        var @event = JsonConvert.DeserializeObject(message, constructedClass);
                        Guid messageId = (@event as dynamic)?.Id;

                        _logger.LogInformation("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling STARTED : MessageId [ {MessageId} ]", _eventBusConfig.ClientInfo, eventName, messageId.ToString());
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType!);
                        await (Task)concreteType.GetMethod("HandleAsync")?.Invoke(handler, new[] { @event })!;
                        _logger.LogInformation("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling COMPLETED : MessageId [ {MessageId} ]", _eventBusConfig.ClientInfo, eventName, messageId.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling ERROR : {HandlingError}", _eventBusConfig.ClientInfo, eventName, ex.Message);
                    }
                }
            }
        }
        else
        {
            _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => No SUBSCRIPTION for event", _eventBusConfig.ClientInfo, eventName);
        }
    }
}