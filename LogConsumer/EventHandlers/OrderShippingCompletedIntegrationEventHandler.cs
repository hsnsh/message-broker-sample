using Hosting.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace LogConsumer.EventHandlers;

public sealed class OrderShippingCompletedIntegrationEventHandler : IIntegrationEventHandler<OrderShippingCompletedIntegrationEvent>
{
    private readonly ILogger<OrderShippingCompletedIntegrationEventHandler> _logger;

    public OrderShippingCompletedIntegrationEventHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OrderShippingCompletedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task HandleAsync(MessageEnvelope<OrderShippingCompletedIntegrationEvent> @event)
    {
        _logger.LogInformation("{Producer} Event[ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            @event.Producer,
            nameof(OrderShippingCompletedIntegrationEvent)[..^"IntegrationEvent".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.RelatedMessageId != null ? @event.RelatedMessageId.Value.ToString() : string.Empty);

        // Simulate a work time
        await Task.Delay(5000);

        await Task.CompletedTask;
    }
}