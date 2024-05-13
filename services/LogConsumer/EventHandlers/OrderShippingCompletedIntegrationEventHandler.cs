using Hosting.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.Logging;

namespace LogConsumer.EventHandlers;

public sealed class OrderShippingCompletedIntegrationEventHandler : IIntegrationEventHandler<OrderShippingCompletedEto>
{
    private readonly ILogger _logger;

    public OrderShippingCompletedIntegrationEventHandler(ILogger logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(MessageEnvelope<OrderShippingCompletedEto> @event)
    {
        _logger.LogInformation("{Producer} Event[ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            @event.Producer,
            nameof(OrderShippingCompletedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        // Simulate a work time
        await Task.Delay(1000);

        await Task.CompletedTask;
    }
}