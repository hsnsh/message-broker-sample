using Multithread.Api.Application;

namespace Multithread.Api.Workers;

public sealed class DeleteWorkerService : BaseWorkerService<DeleteWorkerService>
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DeleteWorkerService(ILogger<DeleteWorkerService> logger, IServiceScopeFactory serviceScopeFactory)
        : base(logger, workerCount: 20)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override void SyncOperation(int workerId)
    {
        Thread.Sleep(50);
        _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | PROCESSING...", nameof(DeleteWorkerService), workerId);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sampleAppService = scope.ServiceProvider.GetRequiredService<ISampleAppService>();
            sampleAppService.DeleteOperation(workerId.ToString()).GetAwaiter().GetResult();

            _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | SUCCESSFULLY PROCESSED", nameof(DeleteWorkerService), workerId);
        }
        catch (Exception e)
        {
            _logger.LogError("{Worker} | WORKER[{WorkerId}] | FAILED: {ErrorMessage}", nameof(DeleteWorkerService), workerId, e.Message);
        }
    }
}