using HsnSoft.Base.Domain.Entities.Events;

namespace NetCoreEventBus.Web.EventManager.Services;

public interface IEventErrorHandlerService 
{
    Task FailedEventConsumedAsync(MessageEnvelope<FailedEto>  input, CancellationToken cancellationToken = default);
}