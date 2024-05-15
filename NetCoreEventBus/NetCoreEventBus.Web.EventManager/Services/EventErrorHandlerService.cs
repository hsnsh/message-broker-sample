using System.Dynamic;
using HsnSoft.Base.Domain.Entities.Events;
using NetCoreEventBus.Shared.Events;
using NetCoreEventBus.Web.EventManager.Infra.Domain;
using Newtonsoft.Json;

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
        string str = input?.Message?.FailedMessageObject?.ToString();
        
        object test = null;
        if (!string.IsNullOrWhiteSpace(str))
        {
            test = JsonConvert.DeserializeObject<ExpandoObject>(str);
            test = JsonConvert.DeserializeObject<OrderStartedEto>(str) as OrderStartedEto;
        }

        #region Re-Generate Integration Event Model for Re-Publish

        string objectSerializedContent = input?.Message?.FailedMessageObject?.ToString();
        
        var refType = typeof(IIntegrationEventMessage);
        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany
            (
                x => x.GetTypes().Where(p => refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false })
            )
            .ToList();

        var eventTypeee = allTypes.FirstOrDefault(x => x.Name.Equals(input.Message.FailedMessageTypeName));
        var originalEvent = JsonConvert.DeserializeObject(objectSerializedContent, eventTypeee);
        var sampleData = ((dynamic)originalEvent)?.OrderId;

        #endregion


        // SAMPLE WORK (work done , 10/second)
        await Task.Delay(10000, cancellationToken);

        // TODO: Save consumed failed event data

        await Task.CompletedTask;
    }
}