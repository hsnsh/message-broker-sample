using Multithread.Api.Application;

namespace Multithread.Api.Workers;

public sealed class InsertWorkerService : BaseHostedService<InsertWorkerService>
{
    private readonly ILogger _logger;

    public InsertWorkerService(ILogger<InsertWorkerService> logger, IServiceScopeFactory serviceScopeFactory)
        : base(logger, serviceScopeFactory, workerCount: 3)
    {
        _logger = logger;
    }

    protected override async Task DoSomethingAsync(IServiceScope scope, int workerId, CancellationToken stopToken)
    {
        Thread.Sleep(50);
        _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | PROCESSING...", nameof(InsertWorkerService), workerId);

        try
        {
            var sampleAppService = scope.ServiceProvider.GetRequiredService<ISampleAppService>();
            await sampleAppService.InsertOperation(workerId, stopToken)!;

            _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | SUCCESSFULLY PROCESSED", nameof(InsertWorkerService), workerId);
        }
        catch (Exception e)
        {
            _logger.LogError("{Worker} | WORKER[{WorkerId}] | FAILED: {ErrorMessage}", nameof(InsertWorkerService), workerId, e.Message);
        }
    }
}