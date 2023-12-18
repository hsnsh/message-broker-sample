#nullable enable
using Base.EventBus.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Base.EventBus.Kafka;

public class EventBusKafka : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusKafka> _logger;
    private readonly EventBusConfig _eventBusConfig;
    private readonly string _bootstrapServer;

    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly CancellationTokenSource _tokenSource;
    private readonly List<Task> _consumerTasks;
    private readonly List<Task> _messageProcessorTasks;

    public EventBusKafka(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, EventBusConfig eventBusConfig, string bootstrapServer)
    {
        _serviceProvider = serviceProvider;
        _logger = loggerFactory.CreateLogger<EventBusKafka>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        _eventBusConfig = eventBusConfig;
        _bootstrapServer = bootstrapServer ?? throw new ArgumentNullException(nameof(bootstrapServer));

        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);
        _tokenSource = new CancellationTokenSource();
        _consumerTasks = new List<Task>();
        _messageProcessorTasks = new List<Task>();
    }

    public async Task PublishAsync(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;
        eventName = TrimEventName(eventName);

        var kafkaProducer = new KafkaProducer(_bootstrapServer, _logger);
        await kafkaProducer.StartSendingMessages(eventName, @event);
    }

    public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        Subscribe(typeof(T), typeof(TH));
    }

    public void Subscribe(Type eventType, Type eventHandlerType)
    {
        if (!eventType.IsAssignableTo(typeof(IntegrationEvent))) throw new TypeAccessException();
        if (!eventHandlerType.IsAssignableTo(typeof(IIntegrationEventHandler))) throw new TypeAccessException();

        var eventName = eventType.Name;
        eventName = TrimEventName(eventName);

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, eventHandlerType.Name);

        _subsManager.AddSubscription(eventType, eventHandlerType);

        _consumerTasks.Add(Task.Run(() =>
        {
            var kafkaConsumer = new KafkaConsumer(_bootstrapServer, _eventBusConfig.SubscriberClientAppName, _logger);
            kafkaConsumer.OnMessageReceived += OnMessageReceived;
            kafkaConsumer.StartReceivingMessages(eventType, eventName, _tokenSource.Token);
        }));
    }

    public void Unsubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = _subsManager.GetEventKey<T>();
        eventName = TrimEventName(eventName);

        _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

        _subsManager.RemoveSubscription<T, TH>();
    }

    public void Dispose()
    {
        _logger.LogInformation("Message Broker Bridge shutting down...");

        _tokenSource.Cancel();
        _tokenSource.Dispose();

        _consumerTasks.RemoveAll(x => x.IsCompleted);
        if (_consumerTasks.Count > 0)
        {
            _logger.LogInformation("Consumer Task Count [ {ConsumerTasksCount} ]", _consumerTasks.Count);

            // Waiting all tasks to finishing their jobs until finish
            Task.WaitAll(_consumerTasks.ToArray());
        }

        _messageProcessorTasks.RemoveAll(x => x.IsCompleted);
        if (_messageProcessorTasks.Count > 0)
        {
            _logger.LogInformation("Message Processor Task Count [ {ProcessorTasks} ]", _messageProcessorTasks.Count);

            // Waiting all tasks to finishing their jobs, but if task processing more time 30 seconds continue
            Task.WaitAll(_messageProcessorTasks.ToArray(), 30000);
        }

        _subsManager.Clear();

        _logger.LogInformation("Message Broker Bridge terminated");
    }

    private void OnMessageReceived(object? sender, object message)
    {
        _messageProcessorTasks.Add(Task.Run(async () =>
        {
            var eventName = message.GetType().Name;
            eventName = TrimEventName(eventName);

            if (_subsManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptions = _subsManager.GetHandlersForEvent(eventName);

                using var scope = _serviceProvider.CreateScope();
                foreach (var subscription in subscriptions)
                {
                    var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                    if (handler == null)
                    {
                        _logger.LogWarning("{ConsumerGroupId} consumed message [ {Topic} ] => No event handler for event", _eventBusConfig.SubscriberClientAppName, eventName);
                        continue;
                    }

                    _logger.LogInformation("{ConsumerGroupId} consumed message [ {Topic} ] => EventId [ {EventId} ] started", _eventBusConfig.SubscriberClientAppName, eventName, (message as IntegrationEvent).Id.ToString());
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(message.GetType());
                    await (Task)concreteType.GetMethod("Handle")?.Invoke(handler, new[] { message })!;
                    _logger.LogInformation("{ConsumerGroupId} consumed message [ {Topic} ] => EventId [ {EventId} ] completed", _eventBusConfig.SubscriberClientAppName, eventName, (message as IntegrationEvent).Id.ToString());
                }
            }
            else
            {
                _logger.LogWarning("{ConsumerGroupId} consumed message [ {Topic} ] => No subscription for event", _eventBusConfig.SubscriberClientAppName, eventName);
            }
        }));
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
}