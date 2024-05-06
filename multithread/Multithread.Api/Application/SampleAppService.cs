using JetBrains.Annotations;
using Multithread.Api.Domain;
using Multithread.Api.EntityFrameworkCore;
using Multithread.Api.EntityFrameworkCore.Core.Repositories;

namespace Multithread.Api.Application;

public sealed class SampleAppService : ISampleAppService
{
    private readonly ILogger<SampleAppService> _logger;

    // private readonly IManagerMongoRepository<SampleMongoDbContext, SampleEntity, Guid> _repository;
    private readonly IManagerEfCoreRepository<SampleEfCoreDbContext, SampleEntity, Guid> _repository;

    public SampleAppService(
        // IManagerMongoRepository<SampleMongoDbContext, SampleEntity, Guid> repository,
        IManagerEfCoreRepository<SampleEfCoreDbContext, SampleEntity, Guid> repository,
        ILogger<SampleAppService> logger
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository;
    }

    public async Task<string> InsertOperation(int sampleInput, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} | INSERT[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        // await Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken);
        // var response = Guid.NewGuid().ToString("N").ToUpper();

        var sample = new SampleEntity(Guid.NewGuid(), sampleInput.ToString());
        await _repository.InsertAsync(sample);
        var response = sample.Id.ToString("N").ToUpper();

        _logger.LogInformation("{Service} | INSERT[{OperationId}] | COMPLETED => ResponseId: {ResponseId}", nameof(SampleAppService), sampleInput, response);
        return response;
    }

    public async Task<bool> DeleteOperation(string sampleInput, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} | DELETE[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        //  Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken).GetAwaiter().GetResult();

        var placed = await _repository.FindAsync(x => x.Name.Equals(sampleInput), cancellationToken: cancellationToken);
        if (placed != null && await _repository.DeleteAsync(placed))
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

        var response = await _repository.FindAsync(x => x.Name.Equals(sampleInput), cancellationToken: cancellationToken);

        _logger.LogInformation("{Service} | FIND[{OperationId}] | COMPLETED => Response: {Response}", nameof(SampleAppService), sampleInput, response?.Id.ToString() ?? string.Empty);

        return response;
    }
}