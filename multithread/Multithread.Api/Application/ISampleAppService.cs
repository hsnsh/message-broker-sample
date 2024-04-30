namespace Multithread.Api.Application;

public interface ISampleAppService
{
    Task<string> SampleOperation(int sampleInput, CancellationToken cancellationToken);
}