using System;
using HsnSoft.Base.Data;
using HsnSoft.Base.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Domain.Services;

public abstract class DomainService : IDomainService, IScopedDependency
{
    [CanBeNull]
    protected IDataFilter DataFilter { get; }

    [CanBeNull]
    protected IStringLocalizerFactory StringLocalizerFactory { get; }

    [CanBeNull]
    protected ILoggerFactory LoggerFactory { get; }

    protected DomainService(IServiceProvider provider = null)
    {
        DataFilter = provider?.GetService<IDataFilter>();
        StringLocalizerFactory = provider?.GetService<IStringLocalizerFactory>();
        LoggerFactory = provider?.GetService<ILoggerFactory>();
    }
}