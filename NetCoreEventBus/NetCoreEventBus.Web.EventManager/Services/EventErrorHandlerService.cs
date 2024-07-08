using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using NetCoreEventBus.Web.EventManager.Infra.Domain;
using Newtonsoft.Json;

namespace NetCoreEventBus.Web.EventManager.Services;

public sealed class EventErrorHandlerService : IEventErrorHandlerService
{
    private readonly IContentGenericRepository<FailedIntegrationEvent> _genericRepository;
    private readonly IEventBus _eventBus;

    public EventErrorHandlerService(IContentGenericRepository<FailedIntegrationEvent> genericRepository, IEventBus eventBus)
    {
        _genericRepository = genericRepository;
        _eventBus = eventBus;
    }

    public async Task FailedEventConsumedAsync(MessageEnvelope<FailedEto> input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(1000, cancellationToken);
        
        var settingsReQueuedLimit = 3;
        if (input.ReQueuedCount >= settingsReQueuedLimit)
        {
            // ERROR LOG

            // SAVE DATABASE WITH ERROR_HANDLING_FAILED, ERROR_HANDLING_COUNT=settingsReQueuedLimit
        }
        else
        {
            await _eventBus.PublishAsync(
                eventMessage: new ReQueuedEto(input.Producer, input.Message.FailedMessageObject, input.Message.FailedMessageTypeName),
                parentMessage: JsonConvert.DeserializeObject<ParentMessageEnvelope>(JsonConvert.SerializeObject(input)),
                isExchangeEvent: false,
                isReQueuePublish: true
            );

            // INFORMATION LOG

            // SAVE DATABASE ERROR_HANDLING_COUNT=settingsReQueuedLimit
        }
    }
}