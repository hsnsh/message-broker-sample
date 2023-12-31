﻿using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Base.Core;
using Base.EventBus.Abstractions;
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
    private readonly RabbitMQEventBusConfig _rabbitMqEventBusConfig;
    private readonly RabbitMQConnectionSettings _rabbitMqConnectionSettings;
    private readonly ILogger<EventBusRabbitMQ> _logger;
    private readonly ITraceAccesor _traceAccessor;
    
    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly IModel? _consumerChannel;

    public EventBusRabbitMQ(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<EventBusRabbitMQ>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        
        _rabbitMqConnectionSettings = _serviceProvider.GetRequiredService<IOptions<RabbitMQConnectionSettings>>().Value;
        _rabbitMqEventBusConfig = _serviceProvider.GetRequiredService<IOptions<RabbitMQEventBusConfig>>().Value;
        _persistentConnection = _serviceProvider.GetRequiredService<IRabbitMQPersistentConnection>();
        _traceAccessor = _serviceProvider.GetService<ITraceAccesor>();
     
        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);

        _consumerChannel = CreateConsumerChannel();
        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    public Task PublishAsync<TEventMessage>(TEventMessage eventMessage, Guid? parentMessageId = null, string? correlationId = null) where TEventMessage : IIntegrationEventMessage
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_rabbitMqConnectionSettings.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning(ex, "RabbitMQ | Could not publish event message : {Event} after {Timeout}s ({ExceptionMessage})", eventMessage, $"{time.TotalSeconds:n1}", ex.Message);
            });

        var eventName = eventMessage.GetType().Name;
        eventName = TrimEventName(eventName);

        _logger.LogDebug("RabbitMQ | Creating channel to publish event name: {EventName}", eventName);

        using (var channel = _persistentConnection.CreateModel())
        {
            _logger.LogDebug("RabbitMQ | Declaring exchange to publish event name: {EventName}", eventName);

            channel.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct"); //Ensure exchange exists while publishing
            
            var @event = new MessageEnvelope<TEventMessage>
            {
                ParentMessageId = parentMessageId,
                MessageId = Guid.NewGuid(),
                MessageTime = DateTime.UtcNow,
                Message = eventMessage,
                Producer = _rabbitMqEventBusConfig.ClientInfo,
                CorrelationId = correlationId ?? _traceAccessor.GetCorrelationId()
            };
            
            _logger.LogDebug("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] STARTED", _rabbitMqEventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());

            var body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

            policy.Execute(() =>
            {
                var properties = channel?.CreateBasicProperties();
                properties!.DeliveryMode = 2; // persistent

                _logger.LogDebug("RabbitMQ | Publishing event: {Event}", @event);

                channel?.BasicPublish(
                    exchange: _rabbitMqEventBusConfig.ExchangeName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });

            _logger.LogDebug("RabbitMQ | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] COMPLETED", _rabbitMqEventBusConfig.ClientInfo, eventName, @event.MessageId.ToString());
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
                exchange: _rabbitMqEventBusConfig.ExchangeName,
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
                exchange: _rabbitMqEventBusConfig.ExchangeName,
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

        channel?.ExchangeDeclare(exchange: _rabbitMqEventBusConfig.ExchangeName, type: "direct");

        return channel;
    }

    private void StartBasicConsume(string eventName)
    {
        _logger.LogInformation("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Subscribed", _rabbitMqEventBusConfig.ClientInfo, eventName);

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

        _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume STARTED", _rabbitMqEventBusConfig.ClientInfo, eventName);
        try
        {
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Received: {Message}", _rabbitMqEventBusConfig.ClientInfo, eventName, message);
            await ProcessEvent(eventName, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume ERROR : {ConsumeError} | {Time}", _rabbitMqEventBusConfig.ClientInfo, eventName, ex.Message, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));
        }

        // Even on exception we take the message off the queue.
        // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
        // For more information see: https://www.rabbitmq.com/dlx.html
        _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
        _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Consume COMPLETED", _rabbitMqEventBusConfig.ClientInfo, eventName);
    }

    private string GetConsumerQueueName(string eventName)
    {
        return $"{_rabbitMqEventBusConfig.ClientInfo}_{TrimEventName(eventName)}";
    }

    private string TrimEventName(string eventName)
    {
        if (_rabbitMqEventBusConfig.DeleteEventPrefix && eventName.StartsWith(_rabbitMqEventBusConfig.EventNamePrefix))
        {
            eventName = eventName[_rabbitMqEventBusConfig.EventNamePrefix.Length..];
        }

        if (_rabbitMqEventBusConfig.DeleteEventSuffix && eventName.EndsWith(_rabbitMqEventBusConfig.EventNameSuffix))
        {
            eventName = eventName[..^_rabbitMqEventBusConfig.EventNameSuffix.Length];
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
                        _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => No HANDLER for event", _rabbitMqEventBusConfig.ClientInfo, eventName);
                        continue;
                    }

                    try
                    {
                        var eventType = _subsManager.GetEventTypeByName($"{_rabbitMqEventBusConfig.EventNamePrefix}{eventName}{_rabbitMqEventBusConfig.EventNameSuffix}");
                      
                        Type genericClass = typeof(MessageEnvelope<>);
                        Type constructedClass = genericClass.MakeGenericType(eventType);
                        var @event = JsonConvert.DeserializeObject(message, constructedClass);
                        Guid messageId = (@event as dynamic)?.Id;

                        _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling STARTED : MessageId [ {MessageId} ]", _rabbitMqEventBusConfig.ClientInfo, eventName, messageId.ToString());
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType!);
                        await (Task)concreteType.GetMethod("HandleAsync")?.Invoke(handler, new[] { @event })!;
                        _logger.LogDebug("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling COMPLETED : MessageId [ {MessageId} ]", _rabbitMqEventBusConfig.ClientInfo, eventName, messageId.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => Handling ERROR : {HandlingError}", _rabbitMqEventBusConfig.ClientInfo, eventName, ex.Message);
                    }
                }
            }
        }
        else
        {
            _logger.LogWarning("RabbitMQ | {ClientInfo} CONSUMER [ {EventName} ] => No SUBSCRIPTION for event", _rabbitMqEventBusConfig.ClientInfo, eventName);
        }
    }
}