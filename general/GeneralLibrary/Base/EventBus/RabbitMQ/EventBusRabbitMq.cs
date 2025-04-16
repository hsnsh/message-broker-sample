﻿using System.Dynamic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GeneralLibrary.Base.Core;
using GeneralLibrary.Base.Domain.Entities.Events;
using GeneralLibrary.Base.EventBus.Logging;
using GeneralLibrary.Base.RabbitMQ;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace GeneralLibrary.Base.EventBus.RabbitMQ;

// ReSharper disable once InconsistentNaming
public sealed class EventBusRabbitMq : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly RabbitMqEventBusConfig _rabbitMqEventBusConfig;
    private readonly RabbitMqConnectionSettings _rabbitMqConnectionSettings;
    private readonly IEventBusLogger _logger;
    private readonly ITraceAccesor _traceAccessor;
    private readonly ICurrentUser _currentUser;

    private readonly IEventBusSubscriptionsManager _subsManager;

    [CanBeNull] private readonly IChannel _consumerChannel;

    private readonly SemaphoreSlim semaphore;

    private bool _disposed;
    private bool _publishing;
    private bool _consuming;

    public EventBusRabbitMq(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _logger = _serviceProvider.GetRequiredService<IEventBusLogger>();

        _rabbitMqConnectionSettings = _serviceProvider.GetRequiredService<IOptions<RabbitMqConnectionSettings>>().Value;
        _rabbitMqEventBusConfig = _serviceProvider.GetRequiredService<IOptions<RabbitMqEventBusConfig>>().Value;
        _persistentConnection = _serviceProvider.GetRequiredService<IRabbitMqPersistentConnection>();
        _traceAccessor = _serviceProvider.GetService<ITraceAccesor>();
        _currentUser = _serviceProvider.GetService<ICurrentUser>();

        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);

        _consumerChannel = CreateConsumerChannelAsync()?.GetAwaiter().GetResult();
        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;

        semaphore = new SemaphoreSlim(_rabbitMqEventBusConfig.ConsumerParallelThreadCount * _rabbitMqEventBusConfig.ConsumerMaxFetchCount);
    }

    public async Task PublishAsync<TEventMessage>(TEventMessage eventMessage, MessageEnvelope parentMessage = null, bool isReQueuePublish = false) where TEventMessage : IIntegrationEventMessage
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        _publishing = true;

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_rabbitMqConnectionSettings.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _publishing = false;
                _logger.LogError("RabbitMQ | Could not publish event message : {Event} after {Timeout}s ({ExceptionMessage})", eventMessage, $"{time.TotalSeconds:n1}", ex.Message);
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
            CorrelationId = parentMessage?.CorrelationId ?? _traceAccessor?.GetCorrelationId(),
            Channel = parentMessage?.Channel ?? _traceAccessor?.GetChannel(),
            UserId = parentMessage?.UserId ?? _currentUser?.Id?.ToString(),
            UserRoleUniqueName = parentMessage?.UserRoleUniqueName ?? (_currentUser?.Roles is { Length: > 0 } ? _currentUser?.Roles.JoinAsString(",") : null),
            HopLevel = parentMessage != null ? parentMessage.HopLevel + 1 : 1,
            IsReQueued = isReQueuePublish || (parentMessage?.IsReQueued ?? false)
        };
        if (@event.IsReQueued)
        {
            @event.ReQueueCount = parentMessage != null ? parentMessage.ReQueueCount + 1 : 0;
        }

        _logger.LogDebug("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] STARTED", _rabbitMqEventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());

        var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

        _logger.LogDebug("RabbitMQ | Creating channel to publish event name: {EventName}", eventName);
        await policy.Execute(async () =>
        {
            await using var publisherChannel = await _persistentConnection.CreateModelAsync()!;

            if (!isReQueuePublish)
            {
                _logger.LogDebug("RabbitMQ | Declaring exchange to publish event name: {EventName}", eventName);
                await publisherChannel.ExchangeDeclareAsync(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct"); //Ensure exchange exists while publishing
            }
            else
            {
                // Direct re-queue, no-exchange
                _logger.LogDebug("RabbitMQ | Declaring queue to publish event name: {EventName}", eventName);
                await publisherChannel?.QueueDeclareAsync(queue: GetConsumerQueueName(eventName),
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null)!;
            }

            _logger.LogDebug("RabbitMQ | Publishing event: {Event}", @event);
            await publisherChannel!.BasicPublishAsync(
                exchange: isReQueuePublish ? "" : _rabbitMqEventBusConfig.ExchangeName,
                routingKey: isReQueuePublish ? GetConsumerQueueName(eventName) : eventName,
                mandatory: true,
                basicProperties: new BasicProperties { DeliveryMode = DeliveryModes.Persistent },
                body: body);
        });

        Thread.Sleep(TimeSpan.FromMilliseconds(50));
        _logger.LogDebug("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] COMPLETED", _rabbitMqEventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());
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

        AddQueueBindForEventSubscription(eventName).GetAwaiter().GetResult();

        _logger.LogInformation("RabbitMQ | Subscribing to event {EventName} with {EventHandler}", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType);
        StartBasicConsume(eventName);
    }

    private async Task AddQueueBindForEventSubscription(string eventName)
    {
        var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
        if (containsKey)
        {
            return;
        }

        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        await _consumerChannel?.QueueDeclareAsync(queue: GetConsumerQueueName(eventName), //Ensure queue exists while consuming
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null)!;

        // take thread count message per consumer
        await _consumerChannel?.BasicQosAsync(0, _rabbitMqEventBusConfig.ConsumerMaxFetchCount, false)!;

        await _consumerChannel?.QueueBindAsync(queue: GetConsumerQueueName(eventName),
            exchange: _rabbitMqEventBusConfig.ExchangeName,
            routingKey: eventName)!;
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
        if (_disposed) return;
        _logger.LogInformation("Message Broker Bridge shutting down...");

        _disposed = true;
        Thread.Sleep(1000); //wait for dispose set

        while (_publishing || _consuming || semaphore.CurrentCount < _rabbitMqEventBusConfig.ConsumerParallelThreadCount * _rabbitMqEventBusConfig.ConsumerMaxFetchCount)
        {
            _logger.LogInformation("Process Count [ {Done}/{All} ] => Publisher and Consumers are waiting...", semaphore.CurrentCount, _rabbitMqEventBusConfig.ConsumerParallelThreadCount * _rabbitMqEventBusConfig.ConsumerMaxFetchCount);
            Thread.Sleep(1000);
        }

        _logger.LogInformation("Process Count [ {Done}/{All} ] => Publisher and Consumers are waiting...", semaphore.CurrentCount, _rabbitMqEventBusConfig.ConsumerParallelThreadCount * _rabbitMqEventBusConfig.ConsumerMaxFetchCount);

        semaphore.Dispose();
        _consumerChannel?.Dispose();
        _subsManager.Clear();

        _logger.LogInformation("Message Broker Bridge terminated");
    }

    private void SubsManager_OnEventRemoved([CanBeNull] object sender, string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnectAsync().GetAwaiter().GetResult();
        }

        using var channel = _persistentConnection.CreateModelAsync()!.GetAwaiter().GetResult();
        channel.QueueUnbindAsync(queue: GetConsumerQueueName(eventName),
            exchange: _rabbitMqEventBusConfig.ExchangeName,
            routingKey: TrimEventName(eventName)).GetAwaiter().GetResult();

        if (_subsManager.IsEmpty)
        {
            _consumerChannel?.CloseAsync().GetAwaiter().GetResult();
        }
    }

    [CanBeNull]
    private async Task<IChannel> CreateConsumerChannelAsync()
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        var channel = await _persistentConnection.CreateModelAsync()!;

        await channel?.ExchangeDeclareAsync(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct")!;

        return channel;
    }

    private async Task StartBasicConsume(string eventName)
    {
        _logger.LogInformation("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Subscribed", _rabbitMqEventBusConfig.ClientInfo, eventName);

        if (_consumerChannel != null)
        {
            for (var i = 0; i < _rabbitMqEventBusConfig.ConsumerParallelThreadCount; i++)
            {
                // Do not using AWAIT , run asynchronously
                Task.Run(async () =>
                {
                    var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
                    consumer.ReceivedAsync += ConsumerReceivedAsync;

                    var consumerTag = await _consumerChannel.BasicConsumeAsync(
                        queue: GetConsumerQueueName(eventName),
                        autoAck: false,
                        consumer: consumer);
                });
            }
        }
        else
        {
            _logger.LogError("RabbitMQ | StartBasicConsume can't call on _consumerChannel == null");
        }
    }

    private async Task ConsumerReceivedAsync([CanBeNull] object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_disposed) return;
        await semaphore.WaitAsync();
        _consuming = true;

        var eventName = eventArgs.RoutingKey;
        if (string.IsNullOrWhiteSpace(eventArgs.Exchange)) // No-Fanout-Exchange direct queue
        {
            eventName = eventArgs.RoutingKey.Split("_").Last();
        }

        eventName = TrimEventName(eventName);

        _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume STARTED", _rabbitMqEventBusConfig.ClientInfo, eventName);

        var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

        // Do not using AWAIT , run asyncronosly
        Task.Run(async () =>
        {
            try
            {
                _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Received: {Message}", _rabbitMqEventBusConfig.ClientInfo, eventName, message);
                ProcessEvent(eventName, message);

                // Even on exception we take the message off the queue.
                // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX).
                // For more information see: https://www.rabbitmq.com/dlx.html
                await _consumerChannel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false)!;
                _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume COMPLETED", _rabbitMqEventBusConfig.ClientInfo, eventName);
            }
            catch (TimeoutException timeProblem)
            {
                // re-try consume
                await _consumerChannel!.BasicNackAsync(eventArgs.DeliveryTag, false, true)!;
                _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume TIMEOUT RETRY: {TimeProblem}", _rabbitMqEventBusConfig.ClientInfo, eventName, timeProblem.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume ERROR : {ConsumeError} | {Time}", _rabbitMqEventBusConfig.ClientInfo, eventName, ex.Message,
                    DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));

                try
                {
                    await ConsumeErrorPublishAsync(ex.Message, eventName, message);

                    // remove from old queue
                    await _consumerChannel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false)!;
                }
                catch (Exception e)
                {
                    // re-try consume
                    await _consumerChannel!.BasicNackAsync(eventArgs.DeliveryTag, false, true);
                    //channel.BasicReject(e.DeliveryTag, true);
                    _logger.LogError("RabbitMQ | {ClientInfo} PRODUCER [ MessageBrokerError ] => FAILED: {ExceptionMessage}", _rabbitMqEventBusConfig.ClientInfo, e.Message);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        _consuming = false;
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
                Guid messageId = ((dynamic)@event)?.MessageId;

                var handler = _serviceProvider.GetService(subscription.HandlerType);
                if (handler == null)
                {
                    _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => No HANDLER for event", _rabbitMqEventBusConfig.ClientInfo, eventName);
                    continue;
                }

                var handleStartTime = DateTimeOffset.UtcNow;
                try
                {
                    _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling STARTED : MessageId [ {MessageId} ]", _rabbitMqEventBusConfig.ClientInfo, eventName, messageId.ToString());
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType!);
                    ((Task)concreteType.GetMethod("HandleAsync")?.Invoke(handler, [@event]))!.GetAwaiter().GetResult();

                    var handleEndTime = DateTimeOffset.UtcNow;
                    var processTime = $"{(handleEndTime - handleStartTime).TotalMilliseconds:0.####}ms";

                    _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling COMPLETED : MessageId [ {MessageId} ], ConsumeHandleWorkingTime [ {ConsumeHandleWorkingTime} ]", _rabbitMqEventBusConfig.ClientInfo,
                        eventName, messageId.ToString(), processTime);
                }
                catch (Exception ex)
                {
                    var handleEndTime = DateTimeOffset.UtcNow;
                    var processTime = $"{(handleEndTime - handleStartTime).TotalMilliseconds:0.####}ms";

                    _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling ERROR : MessageId [ {MessageId} ], ConsumeHandleWorkingTime [ {ConsumeHandleWorkingTime} ], {HandlingError}",
                        _rabbitMqEventBusConfig.ClientInfo, eventName, messageId.ToString(), processTime, ex.Message);
                    throw new Exception(ex.Message);
                }
            }
        }
        else
        {
            _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => No SUBSCRIPTION for event", _rabbitMqEventBusConfig.ClientInfo, eventName);
        }
    }

    private async Task ConsumeErrorPublishAsync([NotNull] string errorMessage, [NotNull] string failedEventName, [NotNull] string failedMessageContent)
    {
        if (!_persistentConnection.IsConnected)
        {
            await _persistentConnection.TryConnectAsync();
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_rabbitMqConnectionSettings.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time) => { _logger.LogWarning("RabbitMQ | Could not publish failed event message : {Event} after {Timeout}s ({ExceptionMessage})", failedMessageContent, $"{time.TotalSeconds:n1}", ex.Message); });

        Type failedMessageType = null;
        dynamic failedMessageObject = null;
        MessageEnvelope failedEnvelopeInfo = null;
        DateTimeOffset? failedMessageEnvelopeTime = null;
        try
        {
            failedMessageType = _subsManager.GetEventTypeByName($"{_rabbitMqEventBusConfig.EventNamePrefix}{failedEventName}{_rabbitMqEventBusConfig.EventNameSuffix}");
            var failedEnvelope = JsonConvert.DeserializeObject<dynamic>(failedMessageContent);
            failedEnvelopeInfo = ((JObject)failedEnvelope)?.ToObject<MessageEnvelope>();

            dynamic dynamicObject = JsonConvert.DeserializeObject<ExpandoObject>(failedMessageContent)!;

            failedMessageEnvelopeTime = dynamicObject.MessageTime;
            failedMessageEnvelopeTime = failedMessageEnvelopeTime?.UtcDateTime;

            failedMessageObject = dynamicObject.Message;
        }
        catch (Exception e)
        {
            errorMessage += "Failed envelope could not convert:" + e.Message;
        }

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

        var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

        _logger.LogDebug("RabbitMQ | Creating channel to publish event name: {EventName}", eventName);
        await policy.Execute(async () =>
        {
            await using var publisherChannel = await _persistentConnection?.CreateModelAsync()!;

            await publisherChannel?.QueueDeclareAsync(queue: GetConsumerQueueName(eventName),
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null)!;

            _logger.LogDebug("RabbitMQ | Publishing event: {Event}", @event);
            await publisherChannel!.BasicPublishAsync(
                exchange: "",
                routingKey: GetConsumerQueueName(eventName),
                mandatory: true,
                basicProperties: new BasicProperties { DeliveryMode = DeliveryModes.Persistent },
                body: body);
        });

        _logger.LogDebug("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] COMPLETED", _rabbitMqEventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());
    }
}