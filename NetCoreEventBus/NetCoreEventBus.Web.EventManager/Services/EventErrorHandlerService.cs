using HsnSoft.Base.Domain.Entities.Events;
using NetCoreEventBus.Web.EventManager.Infra.Domain;

namespace NetCoreEventBus.Web.EventManager.Services;

public sealed class EventErrorHandlerService : IEventErrorHandlerService
{
    private readonly IContentGenericRepository<FailedIntegrationEvent> _genericRepository;

    public EventErrorHandlerService(IContentGenericRepository<FailedIntegrationEvent> genericRepository)
    {
        _genericRepository = genericRepository;
    }

    public async Task FailedEventConsumedAsync(MessageEnvelope<FailedEventEto> input, CancellationToken cancellationToken = default)
    {
        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(10000, cancellationToken);

        // TODO: Save consumed failed event data

        await Task.CompletedTask;
    }
}