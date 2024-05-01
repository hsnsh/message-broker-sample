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

    public async Task<string> SampleOperation(int sampleInput, CancellationToken cancellationToken)
    {
        var sampleWorkTime = new Random().Next(1, 10) * 1000;
        await Task.Delay(100, cancellationToken);
        _logger.LogDebug("{Service} | OPERATION[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        Thread.Sleep(sampleWorkTime);
        var response = Guid.NewGuid().ToString("N").ToUpper();

        // var placed = await _sampleManager.InsertAsync(new SampleEntity(Guid.NewGuid(), sampleInput.ToString()));
        // await _sampleManager.SaveChangesAsync(cancellationToken);
        // var response = placed.Id.ToString("N").ToUpper();
        
        _logger.LogDebug("{Service} | OPERATION[{OperationId}] | COMPLETED => ResponseId: {ResponseId}", nameof(SampleAppService), sampleInput, response);

        return response;
    }
}