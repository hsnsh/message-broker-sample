using Multithread.Api.Domain;

namespace Multithread.Api.Application;

public interface ISampleAppService
{
    Task<string> InsertOperation(int sampleInput, CancellationToken cancellationToken);
    Task<bool> DeleteOperation(string sampleInput, CancellationToken cancellationToken);
    Task<bool> UpdateOperation(string sampleInput, CancellationToken cancellationToken);
    
    Task<SampleEntity> FindOperation(string sampleInput, CancellationToken cancellationToken);
}