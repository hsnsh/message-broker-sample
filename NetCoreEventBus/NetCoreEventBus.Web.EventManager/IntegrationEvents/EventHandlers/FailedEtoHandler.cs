using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using NetCoreEventBus.Web.EventManager.Services;

namespace NetCoreEventBus.Web.EventManager.IntegrationEvents.EventHandlers;

public class FailedEtoHandler : IIntegrationEventHandler<FailedEto>
{
    private readonly IEventErrorHandlerService _eventErrorHandlerService;

    public FailedEtoHandler(IEventErrorHandlerService eventErrorHandlerService)
    {
        _eventErrorHandlerService = eventErrorHandlerService;
    }

    public async Task HandleAsync(MessageEnvelope<FailedEto> @event) => await _eventErrorHandlerService.FailedEventConsumedAsync(@event);
}