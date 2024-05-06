using Multithread.Api.Application;

namespace Multithread.Api.Workers;

public sealed class UpdateWorkerService : BaseHostedService<UpdateWorkerService>
{
    private readonly ILogger _logger;

    public UpdateWorkerService(ILogger<UpdateWorkerService> logger, IServiceScopeFactory serviceScopeFactory)
        : base(logger, serviceScopeFactory, workerCount: 50)
    {
        _logger = logger;
    }

    protected override async Task DoSomethingAsync(IServiceScope scope, int workerId, CancellationToken stopToken)
    {
        Thread.Sleep(50);
        _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | PROCESSING...", nameof(UpdateWorkerService), workerId);

        try
        {
            var sampleAppService = scope.ServiceProvider.GetRequiredService<ISampleAppService>();
            await sampleAppService.UpdateOperation(workerId.ToString(), stopToken)!;

            _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | SUCCESSFULLY PROCESSED", nameof(UpdateWorkerService), workerId);
        }
        catch (Exception e)
        {
            _logger.LogError("{Worker} | WORKER[{WorkerId}] | FAILED: {ErrorMessage}", nameof(UpdateWorkerService), workerId, e.Message);
        }
    }
}