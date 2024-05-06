using JetBrains.Annotations;
using Multithread.Api.Domain;

namespace Multithread.Api.Application;

public sealed class SampleAppService : ISampleAppService
{
    private readonly ILogger<SampleAppService> _logger;

    private readonly IContentGenericRepository<SampleEntity> _genericRepository;

    public SampleAppService(
        IContentGenericRepository<SampleEntity> genericRepository,
        ILogger<SampleAppService> logger
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _genericRepository = genericRepository;
    }

    public async Task<string> InsertOperation(int sampleInput)
    {
        _logger.LogDebug("{Service} | INSERT[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        var sample = new SampleEntity(Guid.NewGuid(), sampleInput.ToString());
        await _genericRepository.InsertAsync(sample);
        var response = sample.Id.ToString("N").ToUpper();

        _logger.LogInformation("{Service} | INSERT[{OperationId}] | COMPLETED => ResponseId: {ResponseId}", nameof(SampleAppService), sampleInput, response);
        await Task.Delay(new Random().Next(1, 5) * 1000);
        return response;
    }

    public async Task<bool> UpdateOperation(string sampleInput)
    {
        _logger.LogDebug("{Service} | UPDATE[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        var res = await _genericRepository.GetListAsync(x => x.Name.Equals(sampleInput));
        if (res is { Count: > 0 })
        {
            foreach (var item in res)
            {
                item.Name = $"{item.Name} updated";
            }

            await _genericRepository.UpdateManyAsync(res);
        }

        _logger.LogInformation("{Service} | UPDATE[{OperationId}] | COMPLETED => Response: {Response}", nameof(SampleAppService), sampleInput, true);
        await Task.Delay(new Random().Next(1, 5) * 1000);
        return true;
    }

    public async Task<bool> DeleteOperation(string sampleInput)
    {
        _logger.LogDebug("{Service} | DELETE[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        sampleInput = $"{sampleInput} updated";
        var res = await _genericRepository.GetListAsync(x => x.Name.Equals(sampleInput));
        if (res is { Count: > 0 })
        {
            await _genericRepository.DeleteManyAsync(res);
        }

        _logger.LogInformation("{Service} | DELETE[{OperationId}] | COMPLETED => Response: {Response}", nameof(SampleAppService), sampleInput, true);
        await Task.Delay(new Random().Next(1, 5) * 1000);
        return true;
    }

    [ItemCanBeNull]
    public async Task<SampleEntity> FindOperation(string sampleInput)
    {
        _logger.LogDebug("{Service} | FIND[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        var response = await _genericRepository.GetListAsync(x => x.Name.Equals(sampleInput));

        _logger.LogDebug("{Service} | FIND[{OperationId}] | COMPLETED => Response: {Response}", nameof(SampleAppService), sampleInput, response?.FirstOrDefault()?.Id.ToString() ?? string.Empty);

        return response is { Count: > 0 } ? response.First() : null;
    }
}