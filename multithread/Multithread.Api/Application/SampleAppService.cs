namespace Multithread.Api.Application;

public sealed class SampleAppService : ISampleAppService
{
    private readonly ILogger<SampleAppService> _logger;

    public SampleAppService(ILogger<SampleAppService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> SampleOperation(int sampleInput, CancellationToken cancellationToken)
    {
        var sampleWorkTime = new Random().Next(1, 10) * 1000;
        await Task.Delay(100, cancellationToken);
        _logger.LogDebug("{Service} | OPERATION[{OperationId}] | STARTED", nameof(SampleAppService), sampleInput);

        // using (var scope = _serviceScopeFactory.CreateScope())
        // {
        //  await Task.Delay(sampleWorkTime, stopToken);
        Thread.Sleep(sampleWorkTime);
        // }

        var response = Guid.NewGuid().ToString("N").ToUpper();
        _logger.LogDebug("{Service} | OPERATION[{OperationId}] | COMPLETED => ResponseId: {ResponseId}", nameof(SampleAppService), sampleInput, response);

        return response;
    }
}