using Hosting.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Logging;
using OrderAPI.Services;

namespace OrderAPI.EventHandlers;

public sealed class OrderShippingCompletedIntegrationEventHandler : IIntegrationEventHandler<OrderShippingCompletedEto>
{
    private readonly ILogger _logger;
    private readonly IOrderService _orderService;

    public OrderShippingCompletedIntegrationEventHandler(ILogger logger,
        IOrderService orderService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    public async Task HandleAsync(MessageEnvelope<OrderShippingCompletedEto> @event)
    {
        _logger.LogInformation("{Producer} Event[ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            @event.Producer,
            nameof(OrderShippingCompletedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _orderService.SetParentIntegrationEvent(@event);
        await _orderService.OrderShippingCompletedAsync(@event.Message);
    }
}