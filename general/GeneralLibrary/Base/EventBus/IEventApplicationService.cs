using GeneralLibrary.Base.Domain.Entities.Events;

namespace GeneralLibrary.Base.EventBus;

public interface IEventApplicationService
{
    public void SetParentIntegrationEvent<T>(MessageEnvelope<T> @event) where T : IIntegrationEventMessage;
}