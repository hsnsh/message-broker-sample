using Multithread.Api.Infrastructure;
using Multithread.Api.Infrastructure.Domain;

namespace Multithread.Api.Application;

public sealed class SampleAppService : ISampleAppService
{
    private readonly ILogger<SampleAppService> _logger;
    private readonly SampleManager<SampleDbContext, SampleEntity> _sampleManager;

    public SampleAppService(ILogger<SampleAppService> logger, SampleManager<SampleDbContext, SampleEntity> sampleManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sampleManager = sampleManager;
    }

    public async Task<string> InsertOperation(int sampleInput, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        _logger.LogDebug("{Service} | INSERT[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        string response;

        Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken).GetAwaiter().GetResult();
        response = Guid.NewGuid().ToString("N").ToUpper();

        // var placed = _sampleManager.InsertAsync(new SampleEntity(Guid.NewGuid(), sampleInput.ToString()), cancellationToken).GetAwaiter().GetResult();
        // response = placed.Id.ToString("N").ToUpper();

        _logger.LogDebug("{Service} | INSERT[{OperationId}] | COMPLETED => ResponseId: {ResponseId}", nameof(SampleAppService), sampleInput, response);
        return response;
    }

    public async Task<bool> DeleteOperation(string sampleInput, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        _logger.LogInformation("{Service} | DELETE[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        Task.Delay(new Random().Next(1, 10) * 1000, cancellationToken).GetAwaiter().GetResult();

        // _sampleManager.DeleteDirectAsync(x => x.Name.Equals(sampleInput), cancellationToken).GetAwaiter().GetResult();

        _logger.LogInformation("{Service} | DELETE[{OperationId}] | COMPLETED => Response: {Response}", nameof(SampleAppService), sampleInput, true);

        return true;
    }
}