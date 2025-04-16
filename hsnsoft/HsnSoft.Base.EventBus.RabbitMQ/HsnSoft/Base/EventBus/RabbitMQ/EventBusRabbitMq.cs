﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using HsnSoft.Base.Tracing;
using HsnSoft.Base.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

// ReSharper disable once InconsistentNaming
public sealed class EventBusRabbitMq : IEventBus, IDisposable
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly RabbitMqEventBusConfig _rabbitMqEventBusConfig;
    private readonly IEventBusLogger _logger;
    private readonly ITraceAccesor _traceAccessor;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBusSubscriptionManager _subsManager;

    private static ushort MaxConsumerParallelThreadCount { get; set; } = 5;
    private static ushort MaxConsumerMaxFetchCount { get; set; } = 10;
    private readonly int _publishRetryCount = 5;
    private readonly List<RabbitMqConsumer> _consumers;
    private bool _disposed;
    private bool _publishing;

    public EventBusRabbitMq(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _logger = serviceProvider.GetRequiredService<IEventBusLogger>();
        _persistentConnection = serviceProvider.GetRequiredService<IRabbitMqPersistentConnection>();
        _traceAccessor = serviceProvider.GetService<ITraceAccesor>();
        _currentUser = serviceProvider.GetService<ICurrentUser>();

        _rabbitMqEventBusConfig = serviceProvider.GetRequiredService<IOptions<RabbitMqEventBusConfig>>().Value;
        if (_rabbitMqEventBusConfig.ConsumerParallelThreadCount > MaxConsumerParallelThreadCount)
        {
            _rabbitMqEventBusConfig.ConsumerParallelThreadCount = MaxConsumerParallelThreadCount;
        }

        if (_rabbitMqEventBusConfig.ConsumerMaxFetchCount > MaxConsumerMaxFetchCount)
        {
            _rabbitMqEventBusConfig.ConsumerMaxFetchCount = MaxConsumerMaxFetchCount;
        }

        _subsManager = serviceProvider.GetService<IEventBusSubscriptionManager>();
        _subsManager.EventNameGetter = TrimEventName;

        _consumers = new List<RabbitMqConsumer>();
    }

    public async Task PublishAsync<TEventMessage>(TEventMessage eventMessage, ParentMessageEnvelope parentMessage = null, string correlationId = null, bool isExchangeEvent = true, bool isReQueuePublish = false)
        where TEventMessage : IIntegrationEventMessage
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        _publishing = true;

        var eventName = eventMessage.GetType().Name;
        eventName = TrimEventName(eventName);

        var produceTime = DateTime.UtcNow;
        var @event = new MessageEnvelope<TEventMessage>
        {
            ParentMessageId = parentMessage?.MessageId,
            MessageId = Guid.NewGuid(),
            MessageTime = produceTime,
            Message = eventMessage,
            Producer = _rabbitMqEventBusConfig.ConsumerClientInfo,
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

        _logger.LogDebug("{BrokerName} | PRODUCER {ClientInfo} EVENT [ {EventName} ] => MessageId [ {MessageId} ] {OperationStatus}", "RabbitMQ", _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, @event.MessageId.ToString(),
            "STARTED");

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_publishRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _publishing = false;
                _logger.LogError("{BrokerName} | Could not publish event message : {Event} after {Timeout}s ({ExceptionMessage})", "RabbitMQ", eventMessage, $"{time.TotalSeconds:n1}", ex.Message);

                // Persistent Log
                _logger.EventBusErrorLog(new ProduceMessageLogModel(
                    LogId: Guid.NewGuid().ToString(),
                    CorrelationId: @event.CorrelationId,
                    Facility: EventBusLogFacility.PRODUCE_EVENT_ERROR.ToString(),
                    ProduceDateTimeUtc: produceTime,
                    MessageLog: new MessageLogDetail(
                        EventType: eventName,
                        HopLevel: @event.HopLevel,
                        ParentMessageId: @event.ParentMessageId,
                        MessageId: @event.MessageId,
                        MessageTime: @event.MessageTime,
                        Message: @event.Message,
                        UserInfo: new EventUserDetail(
                            UserId: @event.UserId,
                            Role: @event.UserRoleUniqueName
                        )),
                    ProduceDetails: $"Message publish error: {ex.Message}"));
            });

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

        policy.Execute(() =>
        {
            using var publisherChannel = _persistentConnection.CreateModel();

            var publishQueueName = string.Empty;
            if (!isReQueuePublish && isExchangeEvent)
            {
                publisherChannel.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct"); //Ensure exchange exists while publishing
            }
            else
            {
                publishQueueName = eventName.Equals("ReQueued")
                    ? EventNameHelper.GetConsumerReQueuedEventQueueName((eventMessage as ReQueuedEto).ReQueuedMessageEnvelopeConsumer, eventName)
                    : EventNameHelper.GetConsumerClientEventQueueName(_rabbitMqEventBusConfig, eventName);

                // Direct re-queue, no-exchange
                publisherChannel?.QueueDeclare(queue: publishQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
            }

            var properties = publisherChannel?.CreateBasicProperties();
            properties!.DeliveryMode = (int)DeliveryMode.Persistent;

            publisherChannel.BasicPublish(
                exchange: !isReQueuePublish && isExchangeEvent ? _rabbitMqEventBusConfig.ExchangeName : "",
                routingKey: !isReQueuePublish && isExchangeEvent ? eventName : publishQueueName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        });

        Thread.Sleep(TimeSpan.FromMilliseconds(50));
        _logger.LogDebug("{BrokerName} | PRODUCER {ClientInfo} EVENT [ {EventName} ] => MessageId [ {MessageId} ] {OperationStatus}", "RabbitMQ", _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, @event.MessageId.ToString(),
            "COMPLETED");
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

            var consumerQueueName = EventNameHelper.GetConsumerClientEventQueueName(_rabbitMqEventBusConfig, eventName);
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct");

                channel.QueueDeclare(queue: consumerQueueName, //Ensure queue exists while consuming
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                channel.QueueBind(queue: consumerQueueName,
                    exchange: _rabbitMqEventBusConfig.ExchangeName,
                    routingKey: eventName);
            }
        }

        _logger.LogDebug("{BrokerName} | Subscribing to event {EventName} with {EventHandler}", "RabbitMQ", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType);

        for (int i = 0; i < _rabbitMqEventBusConfig.ConsumerParallelThreadCount; i++)
        {
            var rabbitMqConsumer = new RabbitMqConsumer(_serviceScopeFactory, _persistentConnection, _subsManager, _rabbitMqEventBusConfig, _logger);
            rabbitMqConsumer.StartBasicConsume(eventName);

            _consumers.Add(rabbitMqConsumer);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _logger.LogInformation("{BrokerName} | {OperationStatus}", "RabbitMQ", "TERMINATING");

        _logger.LogDebug("{BrokerName} | Consumers terminating...", "RabbitMQ");
        Task.WaitAll(_consumers.Select(consumer => Task.Run(consumer.Dispose)).ToArray());
        _logger.LogDebug("{BrokerName} | Consumers terminated", "RabbitMQ");

        _logger.LogDebug("{BrokerName} | Publisher terminating...", "RabbitMQ");
        while (_publishing)
        {
            _logger.LogDebug("{BrokerName} | Publisher wait processing...", "RabbitMQ");
            Thread.Sleep(1000);
        }

        _logger.LogDebug("{BrokerName} | Publisher terminated", "RabbitMQ");

        _subsManager.Clear();
        _consumers.Clear();

        if (_persistentConnection?.IsConnected == true)
        {
            _persistentConnection?.Dispose();
        }

        _logger.LogInformation("{BrokerName} | {OperationStatus}", "RabbitMQ", "TERMINATED");
    }

    private string TrimEventName(string eventName) => EventNameHelper.TrimEventName(_rabbitMqEventBusConfig, eventName);
}