namespace Multithread.Api.Application;

public interface ISampleAppService
{
    Task<string> InsertOperation(int sampleInput, CancellationToken cancellationToken);
    Task<bool> DeleteOperation(string sampleInput, CancellationToken cancellationToken);
}