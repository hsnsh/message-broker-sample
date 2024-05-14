using HsnSoft.Base.Domain.Entities.Events;

namespace NetCoreEventBus.Web.EventManager.Services;

public interface IEventErrorHandlerService 
{
    Task FailedEventConsumedAsync(MessageBrokerErrorEto input, CancellationToken cancellationToken = default);
}