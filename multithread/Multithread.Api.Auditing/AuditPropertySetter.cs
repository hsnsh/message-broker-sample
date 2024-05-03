using Multithread.Api.Auditing.Contracts;
using Multithread.Api.Core;
using Multithread.Api.Core.Security;

namespace Multithread.Api.Auditing;

public class AuditPropertySetter : IAuditPropertySetter, IScopedDependency
{
    public AuditPropertySetter(ICurrentUser currentUser)
    {
        CurrentUser = currentUser;
    }

    protected ICurrentUser CurrentUser { get; }

    public void SetCreationProperties(object targetObject)
    {
        SetCreationTime(targetObject);
        SetCreatorId(targetObject);
    }

    public void SetModificationProperties(object targetObject)
    {
        SetLastModificationTime(targetObject);
        SetLastModifierId(targetObject);
    }

    public void SetDeletionProperties(object targetObject)
    {
        SetDeletionTime(targetObject);
        SetDeleterId(targetObject);
    }

    protected virtual void SetCreationTime(object targetObject)
    {
        if (!(targetObject is IHasCreationTime objectWithCreationTime))
        {
            return;
        }

        if (objectWithCreationTime.CreationTime == default)
        {
            objectWithCreationTime.CreationTime = DateTime.UtcNow;
        }
    }

    protected virtual void SetCreatorId(object targetObject)
    {
        if (!CurrentUser.Id.HasValue)
        {
            return;
        }

        if (targetObject is IMayHaveCreator mayHaveCreatorObject)
        {
            if (mayHaveCreatorObject.CreatorId.HasValue && mayHaveCreatorObject.CreatorId.Value != default)
            {
                return;
            }

            mayHaveCreatorObject.CreatorId = CurrentUser.Id;
        }
    }

    protected virtual void SetLastModificationTime(object targetObject)
    {
        if (targetObject is IHasModificationTime objectWithModificationTime)
        {
            objectWithModificationTime.LastModificationTime = DateTime.UtcNow;
        }
    }

    protected virtual void SetLastModifierId(object targetObject)
    {
        if (!(targetObject is IModificationAuditedObject modificationAuditedObject))
        {
            return;
        }

        if (!CurrentUser.Id.HasValue)
        {
            modificationAuditedObject.LastModifierId = null;
            return;
        }

        modificationAuditedObject.LastModifierId = CurrentUser.Id;
    }

    protected virtual void SetDeletionTime(object targetObject)
    {
        if (targetObject is IHasDeletionTime objectWithDeletionTime)
        {
            if (objectWithDeletionTime.DeletionTime == null)
            {
                objectWithDeletionTime.DeletionTime = DateTime.UtcNow;
            }
        }
    }

    protected virtual void SetDeleterId(object targetObject)
    {
        if (!(targetObject is IDeletionAuditedObject deletionAuditedObject))
        {
            return;
        }

        if (deletionAuditedObject.DeleterId != null)
        {
            return;
        }

        if (!CurrentUser.Id.HasValue)
        {
            deletionAuditedObject.DeleterId = null;
            return;
        }

        deletionAuditedObject.DeleterId = CurrentUser.Id;
    }
}