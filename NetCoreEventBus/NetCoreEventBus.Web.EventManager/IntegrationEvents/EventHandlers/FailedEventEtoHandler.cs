using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using NetCoreEventBus.Web.EventManager.Services;

namespace NetCoreEventBus.Web.EventManager.IntegrationEvents.EventHandlers;

public class FailedEventEtoHandler : IIntegrationEventHandler<FailedEventEto>
{
    private readonly IEventErrorHandlerService _eventErrorHandlerService;

    public FailedEventEtoHandler(IEventErrorHandlerService eventErrorHandlerService)
    {
        _eventErrorHandlerService = eventErrorHandlerService;
    }

    public async Task HandleAsync(MessageEnvelope<FailedEventEto> @event) => await _eventErrorHandlerService.FailedEventConsumedAsync(@event);
}