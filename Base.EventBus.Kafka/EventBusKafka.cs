#nullable enable
using Base.EventBus.Kafka.Converters;
using Base.EventBus.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Base.EventBus.Kafka;

public class EventBusKafka : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusKafka> _logger;
    private readonly EventBusConfig _eventBusConfig;
    private readonly string _bootstrapServer;

    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly JsonSerializerSettings _options = DefaultJsonOptions.Get();
    private readonly CancellationTokenSource _tokenSource;
    private readonly List<Task> consumerTasks;

    public EventBusKafka(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, EventBusConfig eventBusConfig, string bootstrapServer)
    {
        _serviceProvider = serviceProvider;
        _logger = loggerFactory.CreateLogger<EventBusKafka>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        _eventBusConfig = eventBusConfig;
        _bootstrapServer = bootstrapServer ?? throw new ArgumentNullException(nameof(bootstrapServer));

        _subsManager = new InMemoryEventBusSubscriptionsManager(TrimEventName);
        _tokenSource = new CancellationTokenSource();
        consumerTasks = new List<Task>();
    }

    public async Task Publish(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;
        eventName = TrimEventName(eventName);

        var kafkaProducer = new KafkaProducer(_bootstrapServer, _logger);
        await kafkaProducer.StartSendingMessages(eventName, @event);
    }

    public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var eventName = typeof(T).Name;
        eventName = TrimEventName(eventName);

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);

        _subsManager.AddSubscription<T, TH>();

        consumerTasks.Add(Task.Run(async () =>
        {
            var kafkaConsumer = new KafkaConsumer(_bootstrapServer, _eventBusConfig.SubscriberClientAppName, _logger);
            kafkaConsumer.OnMessageReceived += OnMessageReceived;
            kafkaConsumer.StartReceivingMessages<T>(eventName, _tokenSource.Token);
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

        // Wait all tasks to finish their jobs;
        Console.WriteLine("Waiting all tasks to finishing their jobs");
        consumerTasks.RemoveAll(x => x.IsCompleted);
        if (consumerTasks.Count > 0)
        {
            Task.WaitAll(consumerTasks.ToArray(), 20000);
            Console.WriteLine("all tasks to finished their jobs");
        }

        _subsManager.Clear();
    }

    private async void OnMessageReceived(object? sender, IntegrationEvent message)
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

                var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(message.GetType());
                await (Task)concreteType.GetMethod("Handle")?.Invoke(handler, new object[] { message })!;
            }
        }
        else
        {
            _logger.LogWarning("{ConsumerGroupId} consumed message [ {Topic} ] => No subscription for event", _eventBusConfig.SubscriberClientAppName, eventName);
        }
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