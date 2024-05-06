using JetBrains.Annotations;
using Multithread.Api.Domain;
using Multithread.Api.EntityFrameworkCore;
using Multithread.Api.EntityFrameworkCore.Core.Repositories;

namespace Multithread.Api.Application;

public sealed class SampleAppService : ISampleAppService
{
    private readonly ILogger<SampleAppService> _logger;

    // private readonly IManagerMongoRepository<SampleMongoDbContext, SampleEntity, Guid> _repository;
    private readonly IContentGenericRepository<SampleEntity> _genericRepository;

    public SampleAppService(
        // IManagerMongoRepository<SampleMongoDbContext, SampleEntity, Guid> repository,
        IContentGenericRepository<SampleEntity> genericRepository,
        ILogger<SampleAppService> logger
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _genericRepository = genericRepository;
    }

    public async Task<string> InsertOperation(int sampleInput, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} | INSERT[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        // await Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken);
        // var response = Guid.NewGuid().ToString("N").ToUpper();

        var sample = new SampleEntity(Guid.NewGuid(), sampleInput.ToString());
        await _genericRepository.InsertAsync(sample);
        var response = sample.Id.ToString("N").ToUpper();

        _logger.LogInformation("{Service} | INSERT[{OperationId}] | COMPLETED => ResponseId: {ResponseId}", nameof(SampleAppService), sampleInput, response);
        return response;
    }

    public async Task<bool> DeleteOperation(string sampleInput, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} | DELETE[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        //  Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken).GetAwaiter().GetResult();

        var placed = await _genericRepository.FindAsync(x => x.Name.Equals(sampleInput), cancellationToken: cancellationToken);
        if (placed != null && await _genericRepository.DeleteAsync(placed))
        {
            _logger.LogInformation("{Service} | DELETE[{OperationId}] | COMPLETED => Response: {Response}", nameof(SampleAppService), sampleInput, true);
            return true;
        }

        _logger.LogError("{Service} | DELETE[{OperationId}] | FAILED => NOT FOUND", nameof(SampleAppService), sampleInput);
        return false;
    }

    [ItemCanBeNull]
    public async Task<SampleEntity> FindOperation(string sampleInput, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} | FIND[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        //  Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken).GetAwaiter().GetResult();

        var response = await _genericRepository.FindAsync(x => x.Name.Equals(sampleInput), cancellationToken: cancellationToken);

        _logger.LogInformation("{Service} | FIND[{OperationId}] | COMPLETED => Response: {Response}", nameof(SampleAppService), sampleInput, response?.Id.ToString() ?? string.Empty);

        return response;
    }
}