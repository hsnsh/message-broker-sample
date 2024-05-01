using JetBrains.Annotations;
using Multithread.Api.Domain;
using Multithread.Api.EntityFrameworkCore;

namespace Multithread.Api.Application;

public sealed class SampleAppService : ISampleAppService
{
    private readonly ILogger<SampleAppService> _logger;

    // private readonly MongoRepository<SampleMongoDbContext, SampleEntity> _mongoRepository;
    private readonly EfCoreRepository<SampleEfCoreDbContext, SampleEntity> _efCoreRepository;

    public SampleAppService(ILogger<SampleAppService> logger,
        // MongoRepository<SampleMongoDbContext, SampleEntity> mongoRepository,
        EfCoreRepository<SampleEfCoreDbContext, SampleEntity> efCoreRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // _mongoRepository = mongoRepository;
        _efCoreRepository = efCoreRepository;
    }

    public async Task<string> InsertOperation(int sampleInput, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} | INSERT[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        // await Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken);
        // var response = Guid.NewGuid().ToString("N").ToUpper();

        var sample = new SampleEntity(Guid.NewGuid(), sampleInput.ToString());
        await _efCoreRepository.InsertAsync(sample);
        var response = sample.Id.ToString("N").ToUpper();

        _logger.LogInformation("{Service} | INSERT[{OperationId}] | COMPLETED => ResponseId: {ResponseId}", nameof(SampleAppService), sampleInput, response);
        return response;
    }

    public Task<bool> DeleteOperation(string sampleInput, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} | DELETE[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        //  Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken).GetAwaiter().GetResult();

        _efCoreRepository.DeleteDirect(x => x.Name.Equals(sampleInput));

        _logger.LogInformation("{Service} | DELETE[{OperationId}] | COMPLETED => Response: {Response}", nameof(SampleAppService), sampleInput, true);

        return Task.FromResult(true);
    }

    [ItemCanBeNull]
    public async Task<SampleEntity> FindOperation(string sampleInput, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} | FIND[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        //  Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken).GetAwaiter().GetResult();

        var response = await _efCoreRepository.FindAsync(x => x.Name.Equals(sampleInput), cancellationToken);

        _logger.LogInformation("{Service} | FIND[{OperationId}] | COMPLETED => Response: {Response}", nameof(SampleAppService), sampleInput, response?.Id.ToString() ?? string.Empty);

        return response;
    }
}