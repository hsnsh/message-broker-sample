using Hosting.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using ShipmentAPI.Services;

namespace ShipmentAPI.EventHandlers;

public sealed class OrderShippingStartedIntegrationEventHandler : IIntegrationEventHandler<OrderShippingStartedEto>
{
    private readonly ILogger _logger;
    private readonly IShipmentService _shipmentService;

    public OrderShippingStartedIntegrationEventHandler(ILogger logger,
        IShipmentService shipmentService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _shipmentService = shipmentService ?? throw new ArgumentNullException(nameof(shipmentService));
    }

    public async Task HandleAsync(MessageEnvelope<OrderShippingStartedEto> @event)
    {
        _logger.LogInformation("{Producer} Event[ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            @event.Producer,
            nameof(OrderShippingStartedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _shipmentService.SetParentIntegrationEvent(@event);
        await _shipmentService.OrderShippingStartedAsync(@event.Message);
    }
}