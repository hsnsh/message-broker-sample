using JetBrains.Annotations;
using Multithread.Api.Domain;
using Multithread.Api.MongoDb;

namespace Multithread.Api.Application;

public sealed class SampleAppService : ISampleAppService
{
    private readonly ILogger<SampleAppService> _logger;

    private readonly MongoRepository<SampleMongoDbContext, SampleEntity> _repository;
    // private readonly EfCoreRepository<SampleEfCoreDbContext, SampleEntity> _repository;

    public SampleAppService(
        MongoRepository<SampleMongoDbContext, SampleEntity> repository,
        // EfCoreRepository<SampleEfCoreDbContext, SampleEntity> repository,
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

    public Task<bool> DeleteOperation(string sampleInput, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} | DELETE[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        //  Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken).GetAwaiter().GetResult();

        _repository.DeleteDirect(x => x.Name.Equals(sampleInput));

        _logger.LogInformation("{Service} | DELETE[{OperationId}] | COMPLETED => Response: {Response}", nameof(SampleAppService), sampleInput, true);

        return Task.FromResult(true);
    }

    [ItemCanBeNull]
    public async Task<SampleEntity> FindOperation(string sampleInput, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} | FIND[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        //  Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken).GetAwaiter().GetResult();

        var response = await _repository.FindAsync(x => x.Name.Equals(sampleInput), cancellationToken);

        _logger.LogInformation("{Service} | FIND[{OperationId}] | COMPLETED => Response: {Response}", nameof(SampleAppService), sampleInput, response?.Id.ToString() ?? string.Empty);

        return response;
    }
}