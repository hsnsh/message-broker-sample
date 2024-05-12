using System;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public sealed class RabbitMqConsumer : IDisposable
{
    private const int MaxWaitDisposeTime = 30000;
    private readonly TimeSpan _subscribeRetryTime = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly IEventBusSubscriptionManager _subscriptionsManager;
    private readonly RabbitMqEventBusConfig _rabbitMqEventBusConfig;
    private readonly IEventBusLogger<EventBusLogger> _logger;

    private static readonly object ChannelAckResourceLock = new();
    private readonly SemaphoreSlim _consumerPrefetchSemaphore;
    private readonly IModel _consumerChannel;
    private bool _disposed;

    public RabbitMqConsumer(IServiceScopeFactory serviceScopeFactory,
        IRabbitMqPersistentConnection persistentConnection,
        IEventBusSubscriptionManager subscriptionsManager,
        RabbitMqEventBusConfig rabbitMqEventBusConfig,
        IEventBusLogger<EventBusLogger> logger)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory), "MessageBroker ServiceScopeFactory is null");
        _persistentConnection = persistentConnection;
        _subscriptionsManager = subscriptionsManager;
        _rabbitMqEventBusConfig = rabbitMqEventBusConfig;
        _logger = logger;

        _consumerChannel = CreateConsumerChannel();
        _consumerPrefetchSemaphore = new SemaphoreSlim(_rabbitMqEventBusConfig.ConsumerMaxFetchCount);
    }

    public void StartBasicConsume(string eventName)
    {
        if (_consumerChannel == null)
        {
            _logger.LogError("RabbitMQ | StartBasicConsume can't call on _consumerChannel == null");
            return;
        }

        lock (_consumerChannel)
        {
            _consumerChannel?.BasicQos(0, _rabbitMqEventBusConfig.ConsumerMaxFetchCount, false);
            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.Received += ConsumerReceived;
            _consumerChannel?.BasicConsume(queue: GetConsumerQueueName(eventName), autoAck: false, consumer: consumer);
        }

        _logger.LogInformation("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Subscribed", _rabbitMqEventBusConfig.ClientInfo, eventName);
    }

    private async Task ConsumerReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_disposed)
        {
            // don't use semaphore count until disposed function semaphore count check 
            while (true) { Thread.Sleep(1000); }
        }

        await _consumerPrefetchSemaphore.WaitAsync();

        var eventName = eventArgs.RoutingKey;
        if (string.IsNullOrWhiteSpace(eventArgs.Exchange)) // No-Fanout-Exchange direct queue
        {
            eventName = eventArgs.RoutingKey.Split("_").Last();
        }

        _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume STARTED", _rabbitMqEventBusConfig.ClientInfo, eventName);

        var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

        await Task.Run(() =>
        {
            _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => ConsumerTag: {ConsumerTag}", _rabbitMqEventBusConfig.ClientInfo, eventName, (sender as AsyncEventingBasicConsumer)?.ConsumerTags.FirstOrDefault() ?? string.Empty);

            try
            {
                _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Received: {Message}", _rabbitMqEventBusConfig.ClientInfo, eventName, message);

                ProcessEvent(eventName, message);
                lock (ChannelAckResourceLock)
                {
                    _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                    _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume COMPLETED", _rabbitMqEventBusConfig.ClientInfo, eventName);
                }
            }
            catch (TimeoutException timeProblem)
            {
                // re-try consume
                _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume TIMEOUT RETRY: {TimeProblem}", _rabbitMqEventBusConfig.ClientInfo, eventName, timeProblem.Message);
                TryEnqueueMessageAgainAsync(eventArgs, eventName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume ERROR : {ConsumeError} | {Time}", _rabbitMqEventBusConfig.ClientInfo, eventName, ex.Message, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));

                try
                {
                    ConsumeErrorPublish(ex.Message, eventName, message);

                    // remove from old queue
                    lock (ChannelAckResourceLock)
                    {
                        _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
                        _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Message moved to Error Handler Queue", _rabbitMqEventBusConfig.ClientInfo, eventName);
                    }
                }
                catch (Exception)
                {
                    // re-try consume
                    TryEnqueueMessageAgainAsync(eventArgs, eventName);
                }
            }
            finally
            {
                _consumerPrefetchSemaphore.Release();
            }
        });
    }

    private void ProcessEvent(string eventName, string message)
    {
        eventName = TrimEventName(eventName);
        if (_subscriptionsManager.HasSubscriptionsForEvent(eventName))
        {
            var eventType = _subscriptionsManager.GetEventTypeByName(eventName);

            var genericClass = typeof(MessageEnvelope<>);
            var constructedClass = genericClass.MakeGenericType(eventType!);
            var @event = JsonSerializer.Deserialize(message, constructedClass);
            Guid messageId = ((dynamic)@event)?.MessageId;

            var subscriptions = _subscriptionsManager.GetHandlersForEvent(eventName);
            // AbcEvent => AbcEventLogHandler, AbcEventMailHandler etc. Multiple subscription can be for one Event
            foreach (var subscription in subscriptions)
            {
                using var scope = _serviceScopeFactory.CreateScope(); // because handler type scoped service
                var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                if (handler == null)
                {
                    _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => No HANDLER for event", _rabbitMqEventBusConfig.ClientInfo, eventName);
                    continue;
                }

                var handleStartTime = DateTimeOffset.UtcNow;
                try
                {
                    _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling STARTED : MessageId [ {MessageId} ]", _rabbitMqEventBusConfig.ClientInfo, eventName, messageId.ToString());

                    var eventHandlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    ((Task)eventHandlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEventMessage>.HandleAsync))?.Invoke(handler, new[] { @event }))!.GetAwaiter().GetResult();

                    var handleEndTime = DateTimeOffset.UtcNow;
                    var processTime = $"{(handleEndTime - handleStartTime).TotalMilliseconds:0.####}ms";

                    _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling COMPLETED : MessageId [ {MessageId} ], ConsumeHandleWorkingTime [ {ConsumeHandleWorkingTime} ]", _rabbitMqEventBusConfig.ClientInfo, eventName, messageId.ToString(), processTime);
                }
                catch (Exception ex)
                {
                    var handleEndTime = DateTimeOffset.UtcNow;
                    var processTime = $"{(handleEndTime - handleStartTime).TotalMilliseconds:0.####}ms";

                    _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling ERROR : MessageId [ {MessageId} ], ConsumeHandleWorkingTime [ {ConsumeHandleWorkingTime} ], {HandlingError}", _rabbitMqEventBusConfig.ClientInfo, eventName, messageId.ToString(), processTime, ex.Message);
                    throw new Exception(ex.Message);
                }
            }
        }
        else
        {
            _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => No SUBSCRIPTION for event", _rabbitMqEventBusConfig.ClientInfo, eventName);
        }
    }

    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var channel = _persistentConnection.CreateModel();

        channel.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct");

        return channel;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        var consumerChannelNumber = _consumerChannel?.ChannelNumber.ToString();
        _logger.LogInformation("Consumer channel [{ChannelNo}] shutting down...", consumerChannelNumber);

        var waitCounter = 0;
        while (waitCounter * 1000 < MaxWaitDisposeTime && _consumerPrefetchSemaphore.CurrentCount < _rabbitMqEventBusConfig.ConsumerMaxFetchCount)
        {
            _logger.LogInformation("Consumers channel [{ChannelNo}] Fetch Count [ {Done}/{All} ] => waiting...",
                consumerChannelNumber, _consumerPrefetchSemaphore.CurrentCount, _rabbitMqEventBusConfig.ConsumerMaxFetchCount);
            Thread.Sleep(1000);
            waitCounter++;
        }

        _logger.LogInformation("Consumers channel [{ChannelNo}] Fetch Count [ {Done}/{All} ] => all fetching done",
            consumerChannelNumber, _consumerPrefetchSemaphore.CurrentCount, _rabbitMqEventBusConfig.ConsumerMaxFetchCount);

        _consumerPrefetchSemaphore?.Dispose();
        _consumerChannel?.Dispose();

        _logger.LogInformation("Consumer channel [{ChannelNo}] terminated", consumerChannelNumber);
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

    private void TryEnqueueMessageAgainAsync(BasicDeliverEventArgs eventArgs, string eventName)
    {
        if (_disposed) return;

        _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Adding message to queue again with {Time} seconds delay...", _rabbitMqEventBusConfig.ClientInfo, eventName, $"{_subscribeRetryTime.TotalSeconds:n1}");
        Thread.Sleep(_subscribeRetryTime);
        try
        {
            lock (ChannelAckResourceLock)
            {
                _consumerChannel?.BasicNack(eventArgs.DeliveryTag, false, true);
            }

            _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Message added to queue again", _rabbitMqEventBusConfig.ClientInfo, eventName);
        }
        catch (Exception ex)
        {
            _logger.LogError("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Could not enqueue message again: {Error}", _rabbitMqEventBusConfig.ClientInfo, eventName, ex.Message);
        }
    }

    private void ConsumeErrorPublish([NotNull] string errorMessage, [NotNull] string failedEventName, [NotNull] string failedMessageContent)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning("RabbitMQ | Could not publish failed event message : {Event} after {Timeout}s ({ExceptionMessage})", failedMessageContent, $"{time.TotalSeconds:n1}", ex.Message);
            });

        Type failedMessageType = null;
        dynamic failedMessageObject = null;
        ParentMessageEnvelope failedEnvelopeInfo = null;
        DateTimeOffset? failedMessageEnvelopeTime = null;
        try
        {
            failedMessageType = _subscriptionsManager.GetEventTypeByName($"{_rabbitMqEventBusConfig.EventNamePrefix}{failedEventName}{_rabbitMqEventBusConfig.EventNameSuffix}");
            var failedEnvelope = JsonConvert.DeserializeObject<dynamic>(failedMessageContent);
            failedEnvelopeInfo = ((JObject)failedEnvelope)?.ToObject<ParentMessageEnvelope>();

            dynamic dynamicObject = JsonConvert.DeserializeObject<ExpandoObject>(failedMessageContent)!;

            failedMessageEnvelopeTime = dynamicObject.MessageTime;
            failedMessageEnvelopeTime = failedMessageEnvelopeTime?.UtcDateTime;

            failedMessageObject = dynamicObject.Message;
        }
        catch (Exception e) { errorMessage += "Failed envelope could not convert:" + e.Message; }

        var @event = new MessageEnvelope<MessageBrokerErrorEto>
        {
            ParentMessageId = failedEnvelopeInfo?.MessageId,
            MessageId = Guid.NewGuid(),
            MessageTime = DateTime.UtcNow,
            Message = new MessageBrokerErrorEto(
                ErrorTime: DateTime.UtcNow,
                ErrorMessage: errorMessage,
                FailedEventName: failedEventName,
                FailedMessageTypeName: failedMessageType?.Name,
                FailedMessageObject: failedMessageObject,
                FailedMessageEnvelopeTime: failedMessageEnvelopeTime
            ),
            Producer = _rabbitMqEventBusConfig.ClientInfo,
            CorrelationId = failedEnvelopeInfo?.CorrelationId,
            Channel = failedEnvelopeInfo?.Channel,
            UserId = failedEnvelopeInfo?.UserId,
            UserRoleUniqueName = failedEnvelopeInfo?.UserRoleUniqueName,
            HopLevel = failedEnvelopeInfo != null ? failedEnvelopeInfo.HopLevel + 1 : 1,
            IsReQueued = failedEnvelopeInfo?.IsReQueued ?? false
        };
        if (@event.IsReQueued)
        {
            @event.ReQueueCount = failedEnvelopeInfo?.ReQueueCount ?? 0;
        }

        var eventName = @event.Message.GetType().Name;
        eventName = TrimEventName(eventName);

        _logger.LogDebug("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] STARTED", _rabbitMqEventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

        _logger.LogDebug("RabbitMQ | Creating channel to publish event name: {EventName}", eventName);
        policy.Execute(() =>
        {
            using var publisherChannel = _persistentConnection.CreateModel();

            publisherChannel?.QueueDeclare(queue: GetConsumerQueueName(eventName),
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var properties = publisherChannel?.CreateBasicProperties();
            properties!.DeliveryMode = (int)DeliveryMode.Persistent;

            _logger.LogDebug("RabbitMQ | Publishing event: {Event}", @event);
            publisherChannel.BasicPublish(
                exchange: "",
                routingKey: GetConsumerQueueName(eventName),
                mandatory: true,
                basicProperties: properties,
                body: body);
        });

        _logger.LogDebug("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] COMPLETED", _rabbitMqEventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());
    }
}