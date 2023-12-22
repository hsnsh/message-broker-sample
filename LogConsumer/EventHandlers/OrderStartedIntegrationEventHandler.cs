using Hosting.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace LogConsumer.EventHandlers;

public sealed class OrderStartedIntegrationEventHandler : IIntegrationEventHandler<OrderStartedIntegrationEvent>
{
    private readonly ILogger<OrderStartedIntegrationEventHandler> _logger;

    public OrderStartedIntegrationEventHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OrderStartedIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task HandleAsync(MessageEnvelope<OrderStartedIntegrationEvent> @event)
    {
        _logger.LogInformation("{Producer} Event[ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            @event.Producer,
            nameof(OrderStartedIntegrationEvent)[..^"IntegrationEvent".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.RelatedMessageId != null ? @event.RelatedMessageId.Value.ToString() : string.Empty);

        // Simulate a work time
        await Task.Delay(5000);

        await Task.CompletedTask;
    }
}