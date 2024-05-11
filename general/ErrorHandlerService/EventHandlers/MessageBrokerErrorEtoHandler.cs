using GeneralLibrary.Base.Domain.Entities.Events;
using GeneralLibrary.Base.EventBus;
using GeneralLibrary.Events;
using Newtonsoft.Json;

namespace ErrorHandlerService.EventHandlers;

public sealed class MessageBrokerErrorEtoHandler : IIntegrationEventHandler<MessageBrokerErrorEto>
{
    private readonly IEventBus _eventBus;

    public MessageBrokerErrorEtoHandler(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public async Task HandleAsync(MessageEnvelope<MessageBrokerErrorEto> @event)
    {
        await Task.Delay(1000);

        if (@event.ReQueueCount > 1)
        {
            Console.WriteLine("YETERINCE DENENDI");
            return;
        }

        if (!string.IsNullOrWhiteSpace(@event.Message.FailedMessageTypeName) && @event.Message.FailedMessageObject != null)
        {
            var refType = typeof(IIntegrationEventMessage);
            var eventTypes = typeof(SharedEventsAssemblyMarker).Assembly.GetTypes()
                .Where(p => refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false })
                .ToList();

            var failedMessageType = eventTypes.FirstOrDefault(x => x.Name.Equals(@event.Message.FailedMessageTypeName));

            var failedMessage = @event.Message.FailedMessageObject != null
                ? JsonConvert.DeserializeObject(@event.Message.FailedMessageObject.ToString(), failedMessageType)
                : null;

            Console.WriteLine("HATA NEDIR: {0}, MESSAGE NEDIR:{1}", @event.Message.ErrorMessage, failedMessage ?? string.Empty);

            await _eventBus.PublishAsync(failedMessage,
                parentMessage: JsonConvert.DeserializeObject<MessageEnvelope>(JsonConvert.SerializeObject(@event)),
                isReQueuePublish: true);
        }
    }
}