using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.Order.Services;
using Newtonsoft.Json;

namespace NetCoreEventBus.Web.Order.IntegrationEvents.EventHandlers;

public class ReQueuedEtoHandler : IIntegrationEventHandler<ReQueuedEto>
{
    private readonly IBaseLogger _logger;
    private readonly IEventBus _eventBus;
    private readonly IOrderService _orderService;

    public ReQueuedEtoHandler(IBaseLogger logger, IOrderService orderService, IEventBus eventBus)
    {
        _logger = logger;
        _orderService = orderService;
        _eventBus = eventBus;
    }

    public async Task HandleAsync(MessageEnvelope<ReQueuedEto> @event)
    {
        string objectSerializedContent = @event?.Message?.ReQueuedMessageObject.ToString();

        #region Re-Generate Integration Event Model for Re-Publish

        var refType = typeof(IIntegrationEventMessage);
        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany
            (
                x => x.GetTypes().Where(p => refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false })
            )
            .ToList();

        var eventType = allTypes.FirstOrDefault(x => x.Name.Equals(@event?.Message?.ReQueuedMessageTypeName));
        var originalEvent = JsonConvert.DeserializeObject(objectSerializedContent, eventType);

        #endregion

        var parent = JsonConvert.DeserializeObject<ParentMessageEnvelope>(JsonConvert.SerializeObject(@event));
        switch (@event?.Message?.ReQueuedMessageTypeName)
        {
            case nameof(OrderStartedEto):
            {
                await _eventBus.PublishAsync(originalEvent as OrderStartedEto, parentMessage: parent);
                break;
            }
        }
    }
}