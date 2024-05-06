using Multithread.Api.Application;

namespace Multithread.Api.Workers;

public sealed class DeleteWorkerService : BaseHostedService<DeleteWorkerService>
{
    private readonly ILogger _logger;

    public DeleteWorkerService(ILogger<DeleteWorkerService> logger, IServiceScopeFactory serviceScopeFactory)
        : base(logger, serviceScopeFactory, workerCount: 5)
    {
        _logger = logger;
    }

    protected override async Task DoSomethingAsync(IServiceScope scope, int workerId, CancellationToken stopToken)
    {
        Thread.Sleep(50);
        _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | PROCESSING...", nameof(DeleteWorkerService), workerId);

        try
        {
            var sampleAppService = scope.ServiceProvider.GetRequiredService<ISampleAppService>();
            await sampleAppService.DeleteOperation(workerId.ToString(), stopToken)!;

            _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | SUCCESSFULLY PROCESSED", nameof(DeleteWorkerService), workerId);
        }
        catch (Exception e)
        {
            _logger.LogError("{Worker} | WORKER[{WorkerId}] | FAILED: {ErrorMessage}", nameof(DeleteWorkerService), workerId, e.Message);
        }
    }
}