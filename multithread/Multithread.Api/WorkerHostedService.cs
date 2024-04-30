using System.Diagnostics;
using Multithread.Api.Application;

namespace Multithread.Api;

public class WorkerHostedService : BackgroundService
{
    private readonly ILogger<WorkerHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly List<Task> _messageProcessorTasks;
    private readonly int _workerCount;
    private readonly int _maxTerminatingWaitPeriodForAllWorker = 30000;

    public WorkerHostedService(ILogger<WorkerHostedService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _workerCount = 10;
        _messageProcessorTasks = new List<Task>();
    }

    protected override async Task ExecuteAsync(CancellationToken stopToken)
    {
        _logger.LogInformation("{Worker} | STARTING...", nameof(WorkerHostedService));

        var loopCount = 0;
        while (!stopToken.IsCancellationRequested)
        {
            _logger.LogInformation("{Worker} | WORKER COUNT IS {WorkerCount}", nameof(WorkerHostedService), _workerCount);

            for (var i = 1; i <= _workerCount; i++)
            {
                _messageProcessorTasks.Add(new Task(
                    action: o =>
                    {
                        var processId = (o as ProcessModel)?.ProcessId ?? 0;

                        _logger.LogInformation("{Worker} | WORKER[{WorkerId}] | STARTED", nameof(WorkerHostedService), processId);
                        var stopWatch = Stopwatch.StartNew();

                        // SOME OPERATION
                        DoSomethingAsync(processId, stopToken)
                            .GetAwaiter().GetResult(); // WAIT OPERATION COMPLETED

                        stopWatch.Stop();
                        var timespan = stopWatch.Elapsed;
                        _logger.LogInformation("{Worker} | WORKER[{WorkerId}] | FINISHED ({ProcessTime})sn", nameof(WorkerHostedService), processId, timespan.TotalSeconds.ToString("0.###"));
                    },
                    state: new ProcessModel { ProcessId = i + loopCount * 10 },
                    cancellationToken: stopToken)
                );
            }

            Parallel.For(0, 10, i =>
            {
                _messageProcessorTasks[i].Start();
            });

            await Task.WhenAll(_messageProcessorTasks).ContinueWith(_ =>
            {
                _messageProcessorTasks.RemoveAll(t => t.IsCompleted);
            }, stopToken);

            _logger.LogInformation("{Worker} | ALL WORKER IS AVAILABLE", nameof(WorkerHostedService));
            // loop wait period
            await Task.Delay(1000, stopToken);
            loopCount++;
        }
    }

    private async Task DoSomethingAsync(int workerId, CancellationToken stopToken)
    {
        _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | PROCESSING...", nameof(WorkerHostedService), workerId);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sampleAppService = scope.ServiceProvider.GetService<ISampleAppService>();
            await sampleAppService?.SampleOperation(workerId, stopToken)!;

            _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | SUCCESSFULLY PROCESSED", nameof(WorkerHostedService), workerId);
        }
        catch (Exception e)
        {
            _logger.LogError("{Worker} | WORKER[{WorkerId}] | FAILED: {ErrorMessage}", nameof(WorkerHostedService), workerId, e.Message);
        }
    }

    public override void Dispose()
    {
        _logger.LogInformation("{Worker} | STOPPING...", nameof(WorkerHostedService));
        base.Dispose();

        _messageProcessorTasks.RemoveAll(x => x.IsCompleted);
        if (_messageProcessorTasks.Count > 0)
        {
            _logger.LogInformation("{Worker} | Wait Background Worker Count [ {ProcessorTasks} ]", nameof(WorkerHostedService), _messageProcessorTasks.Count);

            // Waiting all tasks to finishing their jobs, but if task processing more time 30 seconds continue
            Task.WaitAll(_messageProcessorTasks.ToArray(), _maxTerminatingWaitPeriodForAllWorker);
        }

        _logger.LogInformation("{Worker} | STOPPED", nameof(WorkerHostedService));
    }
}