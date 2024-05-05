using System.Diagnostics;

namespace ThreadSafe.Api.Workers;

public abstract class BaseHostedService<TService> : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly List<Task> _messageProcessorTasks;
    private readonly int _workerCount;
    private readonly int _maxTerminatingWaitPeriodForAllWorker = 30000;

    protected BaseHostedService(ILogger logger, IServiceScopeFactory serviceScopeFactory, int workerCount = 10)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _workerCount = workerCount < 1 ? 1 : workerCount;
        _messageProcessorTasks = new List<Task>();
    }

    protected override async Task ExecuteAsync(CancellationToken stopToken)
    {
        _logger.LogInformation("{Worker} | STARTING...", typeof(TService).Name);

        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var loopCount = 0;
            while (!stopToken.IsCancellationRequested)
            {
                _logger.LogInformation("{Worker} | WORKER COUNT IS {WorkerCount}", typeof(TService).Name, _workerCount);


                for (var i = 1; i <= _workerCount; i++)
                {
                    _messageProcessorTasks.Add(new Task(
                        action: o =>
                        {
                            var processId = (o as ProcessModel)?.ProcessId ?? 0;

                            _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | STARTED", typeof(TService).Name, processId);
                            var stopWatch = Stopwatch.StartNew();

                            // SOME OPERATION
                            DoSomethingAsync(scope, processId, stopToken)
                                .GetAwaiter().GetResult(); // WAIT OPERATION COMPLETED

                            stopWatch.Stop();
                            var timespan = stopWatch.Elapsed;
                            _logger.LogDebug("{Worker} | WORKER[{WorkerId}] | FINISHED ({ProcessTime})sn", typeof(TService).Name, processId, timespan.TotalSeconds.ToString("0.###"));
                        },
                        state: new ProcessModel { ProcessId = i + loopCount * _workerCount },
                        cancellationToken: stopToken)
                    );
                }

                Parallel.For(0, _workerCount, i =>
                {
                    _messageProcessorTasks[i].Start();
                });

                await Task.WhenAll(_messageProcessorTasks).ContinueWith(_ =>
                {
                    _messageProcessorTasks.RemoveAll(t => t.IsCompleted);
                    _logger.LogDebug("{Worker} | ALL WORKER IS AVAILABLE", typeof(TService).Name);
                    // loop wait period
                    Thread.Sleep(1000);
                    loopCount++;
                }, stopToken);
            }
        }
    }

    protected abstract Task DoSomethingAsync(IServiceScope scope, int workerId, CancellationToken stopToken);

    public override void Dispose()
    {
        _logger.LogInformation("{Worker} | STOPPING...", typeof(TService).Name);
        base.Dispose();

        _messageProcessorTasks.RemoveAll(x => x.IsCompleted);
        if (_messageProcessorTasks.Count > 0)
        {
            _logger.LogInformation("{Worker} | Wait Background Worker Count [ {ProcessorTasks} ]", typeof(TService).Name, _messageProcessorTasks.Count);

            // Waiting all tasks to finishing their jobs, but if task processing more time 30 seconds continue
            Task.WaitAll(_messageProcessorTasks.ToArray(), _maxTerminatingWaitPeriodForAllWorker);
        }

        _logger.LogInformation("{Worker} | STOPPED", typeof(TService).Name);
    }
}