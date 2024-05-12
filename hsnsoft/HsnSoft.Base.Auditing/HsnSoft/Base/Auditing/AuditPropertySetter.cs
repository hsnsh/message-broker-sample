using System;
using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.Auditing;

public class AuditPropertySetter : IAuditPropertySetter, ITransientDependency
{
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

    private void SetCreationTime(object targetObject)
    {
        if (!(targetObject is IHasCreationTime objectWithCreationTime))
        {
            return;
        }

        if (objectWithCreationTime.CreationTime == default)
        {
            ObjectHelper.TrySetProperty(objectWithCreationTime, x => x.CreationTime, () => DateTime.UtcNow);
        }
    }

    private void SetCreatorId(object targetObject)
    {
        return;
    }

    private void SetLastModificationTime(object targetObject)
    {
        if (targetObject is IHasModificationTime objectWithModificationTime)
        {
            objectWithModificationTime.LastModificationTime = DateTime.UtcNow;
        }
    }

    private void SetLastModifierId(object targetObject)
    {
        return;
    }

    private void SetDeletionTime(object targetObject)
    {
        if (targetObject is IHasDeletionTime objectWithDeletionTime)
        {
            if (objectWithDeletionTime.DeletionTime == null)
            {
                objectWithDeletionTime.DeletionTime = DateTime.UtcNow;
            }
        }
    }

    private void SetDeleterId(object targetObject)
    {
        return;
    }
}