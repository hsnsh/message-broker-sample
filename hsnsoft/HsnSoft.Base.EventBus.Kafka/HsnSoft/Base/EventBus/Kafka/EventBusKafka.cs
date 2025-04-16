﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Kafka.Configs;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.Tracing;
using HsnSoft.Base.Users;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HsnSoft.Base.EventBus.Kafka;

public class EventBusKafka : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventBusLogger _logger;
    private readonly KafkaConnectionSettings _kafkaConnectionSettings;
    private readonly KafkaEventBusConfig _kafkaEventBusConfig;
    private readonly ITraceAccesor _traceAccessor;
    private readonly ICurrentUser _currentUser;

    private readonly IEventBusSubscriptionManager _subsManager;
    private readonly CancellationTokenSource _tokenSource;
    private readonly List<Task> _consumerTasks;
    private readonly List<Task> _messageProcessorTasks;

    public EventBusKafka(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _logger = _serviceProvider.GetRequiredService<IEventBusLogger>();

        _kafkaConnectionSettings = _serviceProvider.GetRequiredService<IOptions<KafkaConnectionSettings>>().Value;
        _kafkaEventBusConfig = _serviceProvider.GetRequiredService<IOptions<KafkaEventBusConfig>>().Value;
        _traceAccessor = _serviceProvider.GetService<ITraceAccesor>();
        _currentUser = _serviceProvider.GetService<ICurrentUser>();

        _subsManager = serviceProvider.GetService<IEventBusSubscriptionManager>();
        _subsManager.EventNameGetter = TrimEventName;

        _tokenSource = new CancellationTokenSource();
        _consumerTasks = new List<Task>();
        _messageProcessorTasks = new List<Task>();
    }

    public async Task PublishAsync<TEventMessage>(TEventMessage eventMessage, ParentMessageEnvelope parentMessage = null, string correlationId = null, bool isExchangeEvent = true, bool isReQueuePublish = false)
        where TEventMessage : IIntegrationEventMessage
    {
        var eventName = eventMessage.GetType().Name;
        eventName = TrimEventName(eventName);

        var kafkaProducer = new KafkaProducer(_kafkaConnectionSettings, _kafkaEventBusConfig, _logger);

        var @event = new MessageEnvelope<TEventMessage>
        {
            ParentMessageId = parentMessage?.MessageId,
            MessageId = Guid.NewGuid(),
            MessageTime = DateTime.UtcNow,
            Message = eventMessage,
            Producer = _kafkaEventBusConfig.ConsumerClientInfo,
            CorrelationId = (correlationId ?? parentMessage?.CorrelationId) ?? _traceAccessor?.GetCorrelationId(),
            Channel = parentMessage?.Channel ?? _traceAccessor?.GetChannel(),
            UserId = parentMessage?.UserId ?? _currentUser?.Id?.ToString(),
            UserRoleUniqueName = parentMessage?.UserRoleUniqueName ?? (_currentUser?.Roles is { Length: > 0 } ? _currentUser?.Roles.JoinAsString(",") : null),
            HopLevel = parentMessage != null ? (ushort)(parentMessage.HopLevel + 1) : (ushort)1,
            ReQueuedCount = parentMessage?.ReQueuedCount ?? 0
        };
        if (isReQueuePublish)
        {
            @event.ReQueuedCount++;
        }

        await kafkaProducer.StartSendingMessages(eventName, @event);
    }

    public void Subscribe<T, TH>(ushort fetchCount = 1) where T : IIntegrationEventMessage where TH : IIntegrationEventHandler<T>
    {
        Subscribe(typeof(T), typeof(TH), fetchCount);
    }

    public void Subscribe(Type eventType, Type eventHandlerType, ushort fetchCount = 1)
    {
        if (!eventType.IsAssignableTo(typeof(IIntegrationEventMessage))) throw new TypeAccessException();
        if (!eventHandlerType.IsAssignableTo(typeof(IIntegrationEventHandler))) throw new TypeAccessException();

        var eventName = eventType.Name;
        eventName = TrimEventName(eventName);

        _logger.LogDebug("Kafka | Subscribing to event {EventName} with {EventHandler}", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType);

        _consumerTasks.Add(Task.Run(() =>
        {
            var kafkaConsumer = new KafkaConsumer(_kafkaConnectionSettings, _kafkaEventBusConfig, _logger);
            kafkaConsumer.OnMessageReceived += OnMessageReceived;
            kafkaConsumer.StartReceivingMessages(eventType, eventName, _tokenSource.Token);
        }));
    }

    public void Dispose()
    {
        _logger.LogDebug("Message Broker Bridge shutting down...");

        _tokenSource.Cancel();
        _tokenSource.Dispose();

        _consumerTasks.RemoveAll(x => x.IsCompleted);
        if (_consumerTasks.Count > 0)
        {
            _logger.LogDebug("Consumer Task Count [ {ConsumerTasksCount} ]", _consumerTasks.Count);

            // Waiting all tasks to finishing their jobs until finish
            Task.WaitAll(_consumerTasks.ToArray());
        }

        _messageProcessorTasks.RemoveAll(x => x.IsCompleted);
        if (_messageProcessorTasks.Count > 0)
        {
            _logger.LogDebug("Message Processor Task Count [ {ProcessorTasks} ]", _messageProcessorTasks.Count);

            // Waiting all tasks to finishing their jobs, but if task processing more time 30 seconds continue
            Task.WaitAll(_messageProcessorTasks.ToArray(), 30000);
        }

        _subsManager.Clear();

        _logger.LogDebug("Message Broker Bridge terminated");
    }

    private void OnMessageReceived([CanBeNull] object sender, KeyValuePair<Type, string> messageObject)
    {
        _messageProcessorTasks.Add(Task.Run(() =>
        {
            var eventName = messageObject.Key.Name;
            eventName = TrimEventName(eventName);

            if (_subsManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                foreach (var subscription in subscriptions)
                {
                    var handler = _serviceProvider.GetService(subscription.HandlerType);
                    if (handler == null)
                    {
                        _logger.LogWarning("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => No HANDLER for event", _kafkaEventBusConfig.ConsumerClientInfo, eventName);
                        continue;
                    }

                    object @event = null;
                    var watch = new Stopwatch();
                    watch.Start();
                    var handleStartTime = DateTimeOffset.UtcNow;
                    try
                    {
                        var genericClass = typeof(MessageEnvelope<>);
                        var constructedClass = genericClass.MakeGenericType(messageObject.Key);
                        @event = JsonConvert.DeserializeObject(messageObject.Value, constructedClass);

                        Guid messageId = ((dynamic)@event)?.MessageId;

                        _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Handling STARTED : MessageId [ {MessageId} ]", _kafkaEventBusConfig.ConsumerClientInfo, eventName, messageId.ToString());
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(messageObject.Key);
                        ((Task)concreteType.GetMethod("HandleAsync")?.Invoke(handler, new[] { @event }))!.GetAwaiter().GetResult();
                        _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Handling COMPLETED : MessageId [ {MessageId} ]", _kafkaEventBusConfig.ConsumerClientInfo, eventName, messageId.ToString());

                        watch.Stop();
                        _logger.EventBusInfoLog(new ConsumeMessageLogModel(
                            LogId: Guid.NewGuid().ToString(),
                            CorrelationId: ((dynamic)@event)?.CorrelationId,
                            Facility: EventBusLogFacility.CONSUME_EVENT_SUCCESS.ToString(),
                            Producer: ((dynamic)@event)?.Producer,
                            ConsumeDateTimeUtc: handleStartTime,
                            MessageLog: new MessageLogDetail(
                                EventType: eventName,
                                HopLevel: ((dynamic)@event)?.HopLevel,
                                ParentMessageId: ((dynamic)@event)?.ParentMessageId,
                                MessageId: ((dynamic)@event)?.MessageId,
                                MessageTime: ((dynamic)@event)?.MessageTime,
                                Message: ((dynamic)@event)?.Message,
                                UserInfo: new EventUserDetail(
                                    UserId: ((dynamic)@event)?.UserId,
                                    Role: ((dynamic)@event)?.UserRoleUniqueName
                                )),
                            ConsumeDetails: "Message handling successfully completed",
                            ConsumeHandleWorkingTime: $"{watch.ElapsedMilliseconds:0.####}ms"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Kafka | CorrelationId: {CorrelationId} {ClientInfo} CONSUMER [ {EventName} ] => Handling ERROR : {HandlingError}", ((dynamic)@event)?.CorrelationId, _kafkaEventBusConfig.ConsumerClientInfo,
                            eventName, ex.Message);

                        watch.Stop();
                        _logger.EventBusErrorLog(new ConsumeMessageLogModel(
                            LogId: Guid.NewGuid().ToString(),
                            CorrelationId: ((dynamic)@event)?.CorrelationId,
                            Facility: EventBusLogFacility.CONSUME_EVENT_ERROR.ToString(),
                            Producer: ((dynamic)@event)?.Producer,
                            ConsumeDateTimeUtc: handleStartTime,
                            MessageLog: new MessageLogDetail(
                                EventType: eventName,
                                HopLevel: ((dynamic)@event)?.HopLevel,
                                ParentMessageId: ((dynamic)@event)?.ParentMessageId,
                                MessageId: ((dynamic)@event)?.MessageId,
                                MessageTime: ((dynamic)@event)?.MessageTime,
                                Message: ((dynamic)@event)?.Message,
                                UserInfo: new EventUserDetail(
                                    UserId: ((dynamic)@event)?.UserId,
                                    Role: ((dynamic)@event)?.UserRoleUniqueName
                                )),
                            ConsumeDetails: $"Handle Error: {ex.Message}",
                            ConsumeHandleWorkingTime: $"{watch.ElapsedMilliseconds:0.####}ms"));
                    }
                }
            }
            else
            {
                _logger.LogWarning("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => No SUBSCRIPTION for event", _kafkaEventBusConfig.ConsumerClientInfo, eventName);
            }
        }));
    }

    private string TrimEventName(string eventName)
    {
        if (_kafkaEventBusConfig.DeleteEventPrefix && eventName.StartsWith(_kafkaEventBusConfig.EventNamePrefix))
        {
            eventName = eventName[_kafkaEventBusConfig.EventNamePrefix.Length..];
        }

        if (_kafkaEventBusConfig.DeleteEventSuffix && eventName.EndsWith(_kafkaEventBusConfig.EventNameSuffix))
        {
            eventName = eventName[..^_kafkaEventBusConfig.EventNameSuffix.Length];
        }

        return eventName;
    }
}