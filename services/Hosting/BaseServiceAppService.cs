using HsnSoft.Base.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Hosting;

public abstract class BaseServiceAppService : ApplicationService, IEventApplicationService
{
    [NotNull]
    protected ILogger Logger { get; }
    
    [NotNull]
    protected IEventBus EventBus { get; }

    [CanBeNull]
    protected ParentMessageEnvelope ParentIntegrationEvent { get; private set; }

    protected BaseServiceAppService(IServiceProvider provider)
    {
        Logger = LoggerFactory.CreateLogger("MessageBrokerSample");
        EventBus = provider.GetRequiredService<IEventBus>();
    }

    public void SetParentIntegrationEvent<T>(MessageEnvelope<T> @event) where T : IIntegrationEventMessage
    {
        ParentIntegrationEvent = JsonConvert.DeserializeObject<ParentMessageEnvelope>(JsonConvert.SerializeObject(@event));
    }
}