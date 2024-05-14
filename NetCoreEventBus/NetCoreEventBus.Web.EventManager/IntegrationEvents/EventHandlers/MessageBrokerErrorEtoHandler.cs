using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging;
using NetCoreEventBus.Web.EventManager.Services;

namespace NetCoreEventBus.Web.EventManager.IntegrationEvents.EventHandlers;

public class MessageBrokerErrorEtoHandler : IIntegrationEventHandler<MessageBrokerErrorEto>
{
    private readonly IBaseLogger _logger;
    private readonly IEventErrorHandlerService _eventErrorHandlerService;

    public MessageBrokerErrorEtoHandler(IBaseLogger logger, IEventErrorHandlerService eventErrorHandlerService)
    {
        _logger = logger;
        _eventErrorHandlerService = eventErrorHandlerService;
    }

    public async Task HandleAsync(MessageEnvelope<MessageBrokerErrorEto> @event)
    {
        _logger.LogInformation(@event.ToString());
        await _eventErrorHandlerService.FailedEventConsumedAsync(@event.Message);
    }
}