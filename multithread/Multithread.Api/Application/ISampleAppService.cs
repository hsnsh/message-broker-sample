using Multithread.Api.Domain;

namespace Multithread.Api.Application;

public interface ISampleAppService
{
    Task<string> InsertOperation(int sampleInput);
    Task<bool> DeleteOperation(string sampleInput);
    Task<bool> UpdateOperation(string sampleInput);
    
    Task<SampleEntity> FindOperation(string sampleInput);
}