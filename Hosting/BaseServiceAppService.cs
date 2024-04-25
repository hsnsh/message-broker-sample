using HsnSoft.Base.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hosting;

public abstract class BaseServiceAppService : BaseApplicationService, IEventApplicationService
{
    [NotNull]
    protected ILogger Logger { get; }
    
    [NotNull]
    protected IEventBus EventBus { get; }

    [CanBeNull]
    protected ParentMessageEnvelope ParentIntegrationEvent { get; private set; }

    protected BaseServiceAppService(IServiceProvider provider)
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        Logger = loggerFactory.CreateLogger("MessageBrokerSample");

        EventBus = provider.GetRequiredService<IEventBus>();
    }

    public void SetParentIntegrationEvent<T>(MessageEnvelope<T> @event) where T : IIntegrationEventMessage
    {
        ParentIntegrationEvent = new ParentMessageEnvelope
        {
            HopLevel = @event.HopLevel,
            MessageId = @event.MessageId,
            CorrelationId = @event.CorrelationId,
            UserId = @event.UserId,
            UserRoleUniqueName = @event.UserRoleUniqueName,
            Channel = @event.Channel,
            Producer = @event.Producer
        };
    }
}