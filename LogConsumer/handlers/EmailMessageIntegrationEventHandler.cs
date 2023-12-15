using Base.EventBus;
using Microsoft.Extensions.Logging;
using Shared;

namespace LogConsumer.handlers;

public sealed class EmailMessageIntegrationEventHandler : IIntegrationEventHandler<EmailMessageIntegrationEvent>
{
    private readonly ILogger<EmailMessageIntegrationEventHandler> _logger;

    public EmailMessageIntegrationEventHandler(ILoggerFactory loggerFactory)
    {
        // _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger = loggerFactory.CreateLogger<EmailMessageIntegrationEventHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task Handle(EmailMessageIntegrationEvent @event)
    {
        var space = typeof(EmailMessageIntegrationEventHandler).Namespace;
        _logger.LogInformation("Handling Integration Event: {IntegrationEventId} at {AppName} - ({@IntegrationEvent})", @event.Id, space, @event);

        // Simulate a work time
        await Task.Delay(1000);

        await Task.CompletedTask;
    }
}