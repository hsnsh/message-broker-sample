using Multithread.Api.Application;

namespace Multithread.Api.Workers;

public sealed class InsertWorkerService : BaseHostedService<InsertWorkerService>
{
    private readonly ILogger<InsertWorkerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public InsertWorkerService(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : base(loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<InsertWorkerService>();
        _scopeFactory = scopeFactory;
    }

    protected override async Task DoSomethingAsync(int workerId, CancellationToken stopToken)
    {
        _logger.LogInformation("{Worker} | WORKER[{WorkerId}] | PROCESSING...", nameof(InsertWorkerService), workerId);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sampleAppService = scope.ServiceProvider.GetService<ISampleAppService>();
            await sampleAppService?.SampleOperation(workerId, stopToken)!;

            _logger.LogInformation("{Worker} | WORKER[{WorkerId}] | SUCCESSFULLY PROCESSED", nameof(InsertWorkerService), workerId);
        }
        catch (Exception e)
        {
            _logger.LogInformation("{Worker} | WORKER[{WorkerId}] | FAILED: {ErrorMessage}", nameof(InsertWorkerService), workerId, e.Message);
        }
    }
}