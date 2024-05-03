using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Multithread.Api.Auditing.Contracts;
using Multithread.Api.Core;
using Multithread.Api.Domain.Core.Entities;
using Multithread.Api.MongoDb.Core.Context;

namespace Multithread.Api.MongoDb.Core;

public abstract class BaseMongoDbContext : MongoDbContext, IScopedDependency
{
    public TimeSpan ClientWaitQueueTimeout => Client.Settings.WaitQueueTimeout;

    protected BaseMongoDbContext(MongoClientSettings clientSettings, string databaseName) : base(clientSettings, databaseName)
    {
        CommandTrackerEvent += CommandTrackerEvent_Tracked;
    }

    protected BaseMongoDbContext(string connectionString) : this(CreateClientSettings(connectionString), MongoUrl.Create(connectionString).DatabaseName)
    {
    }

    private static MongoClientSettings CreateClientSettings(string connectionString, int queryExecutionMaxSeconds = 60)
    {
        ThreadPool.GetMaxThreads(out var maxWt, out var _);

        var mongoUrl = MongoUrl.Create(connectionString);
        var clientSettings = MongoClientSettings.FromConnectionString(mongoUrl.Url);
        clientSettings.MaxConnectionPoolSize = maxWt * 2;

        // In version 2.19, MongoDB team upgraded to LinqProvider.V3, rolling back to V2 until LinQ is stable...
        // https://www.mongodb.com/community/forums/t/issue-with-2-18-to-2-19-nuget-upgrade-of-mongodb-c-driver/211894/2
        clientSettings.LinqProvider = LinqProvider.V2;

        if (queryExecutionMaxSeconds < 1) queryExecutionMaxSeconds = 60;
        clientSettings.WaitQueueTimeout = TimeSpan.FromSeconds(queryExecutionMaxSeconds);

        return clientSettings;
    }

    private void CommandTrackerEvent_Tracked(object sender, MongoEntityEventArgs e)
    {
        switch (e.CommandState)
        {
            case MongoCommandState.Added:
                ApplyBaseConceptsForAddedEntity(e.EntryEntity);
                break;
            case MongoCommandState.Modified:
                ApplyBaseConceptsForModifiedEntity(e.EntryEntity);
                break;
            case MongoCommandState.Deleted:
                ApplyBaseConceptsForDeletedEntity(e.EntryEntity);
                break;
        }
    }

    private void ApplyBaseConceptsForAddedEntity(object entity)
    {
        CheckAndSetId(entity);
        SetCreationTime(entity);
        SetCreatorId(entity);
    }

    private void CheckAndSetId(object targetObject)
    {
        if (targetObject is IEntity<Guid> entityWithGuidId)
        {
            if (entityWithGuidId.Id != default)
            {
                return;
            }

            entityWithGuidId.Id = Guid.NewGuid();
        }
    }

    private void SetCreationTime(object targetObject)
    {
        if (!(targetObject is ICreationAuditedObject objectWithCreationTime))
        {
            return;
        }

        if (objectWithCreationTime.CreationTime == default)
        {
            objectWithCreationTime.CreationTime = DateTime.UtcNow;
        }
    }

    private void SetCreatorId(object targetObject)
    {
        if (!(targetObject is ICreationAuditedObject objectWithCreatorId))
        {
            return;
        }

        if (objectWithCreatorId.CreatorId == null)
        {
            objectWithCreatorId.CreatorId = Guid.NewGuid(); // Todo: CurrentUser => Id
        }
        else if (objectWithCreatorId.CreatorId.HasValue && objectWithCreatorId.CreatorId.Value == default)
        {
            objectWithCreatorId.CreatorId = Guid.NewGuid(); // Todo: CurrentUser => Id
        }
    }

    private void ApplyBaseConceptsForModifiedEntity(object entity)
    {
        SetModificationTime(entity);
        SetModifierId(entity);
    }
    
    private void SetModificationTime(object targetObject)
    {
        if (!(targetObject is IAuditedObject objectWithCreationTime))
        {
            return;
        }

        if (objectWithCreationTime.LastModificationTime == default)
        {
            objectWithCreationTime.LastModificationTime = DateTime.UtcNow;
        }
    }

    private void SetModifierId(object targetObject)
    {
        if (!(targetObject is IAuditedObject objectWithModifierId))
        {
            return;
        }

        if (objectWithModifierId.LastModifierId == null)
        {
            objectWithModifierId.LastModifierId = Guid.NewGuid(); // Todo: CurrentUser => Id
        }
        else if (objectWithModifierId.LastModifierId.HasValue && objectWithModifierId.LastModifierId.Value == default)
        {
            objectWithModifierId.LastModifierId = Guid.NewGuid(); // Todo: CurrentUser => Id
        }
    }

    private void ApplyBaseConceptsForDeletedEntity(object entity)
    {
        if (!(entity is ISoftDelete objectWithSoftDelete))
        {
            return;
        }

        objectWithSoftDelete.IsDeleted = true;
        
        SetModificationTime(entity);
        SetModifierId(entity);
        SetDeletionTime(entity);
        SetDeleterId(entity);
    }
    
    private void SetDeletionTime(object targetObject)
    {
        if (!(targetObject is IFullAuditedObject objectWithCreationTime))
        {
            return;
        }

        if (objectWithCreationTime.DeletionTime == default)
        {
            objectWithCreationTime.DeletionTime = DateTime.UtcNow;
        }
    }

    private void SetDeleterId(object targetObject)
    {
        if (!(targetObject is IFullAuditedObject objectWithDeleterId))
        {
            return;
        }

        if (objectWithDeleterId.DeleterId == null)
        {
            objectWithDeleterId.DeleterId = Guid.NewGuid(); // Todo: CurrentUser => Id
        }
        else if (objectWithDeleterId.DeleterId.HasValue && objectWithDeleterId.DeleterId.Value == default)
        {
            objectWithDeleterId.DeleterId = Guid.NewGuid(); // Todo: CurrentUser => Id
        }
    }
}