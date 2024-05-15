using HsnSoft.Base.Domain.Entities.Events;

namespace NetCoreEventBus.Web.EventManager.Services;

public interface IEventErrorHandlerService 
{
    Task FailedEventConsumedAsync(MessageEnvelope<FailedEventEto>  input, CancellationToken cancellationToken = default);
}