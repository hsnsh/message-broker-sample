using System.Reflection;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.AspNetCore.Security.Claims;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Kafka;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ;
using HsnSoft.Base.Kafka;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.RabbitMQ;
using HsnSoft.Base.Security;
using HsnSoft.Base.Security.Claims;
using HsnSoft.Base.Timing;
using HsnSoft.Base.Tracing;
using HsnSoft.Base.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hosting;

public static class MicroserviceHostExtensions
{
    public static IServiceCollection ConfigureMicroserviceHost(this IServiceCollection services)
    {
        services.AddOptions();

        services.AddBaseMultiTenancyServiceCollection();
        services.AddBaseSecurityServiceCollection();
        services.AddBaseTimingServiceCollection();
        services.AddBaseAuditingServiceCollection();
        services.AddBaseDataServiceCollection();
        services.AddBaseAspNetCoreServiceCollection();
        services.AddBaseAspNetCoreJsonLocalization();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        
        services.AddControllers();

        return services;
    }

    public static IServiceCollection AddMicroserviceEventBus(this IServiceCollection services, IConfiguration configuration, Assembly assembly)
    {
        //  services.AddKafkaEventBus(configuration);
        services.AddRabbitMqEventBus(configuration);

        // Add All Event Handlers
        services.AddEventHandlers(assembly);

        return services;
    }

    private static void AddKafkaEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration objects
        services.Configure<KafkaConnectionSettings>(configuration.GetSection("Kafka:Connection"));
        services.Configure<KafkaEventBusConfig>(configuration.GetSection("Kafka:EventBus"));

        // Add event bus instances
        services.AddHttpContextAccessor();
        services.AddSingleton<ICurrentPrincipalAccessor, HttpContextCurrentPrincipalAccessor>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<ITraceAccesor, HttpContextTraceAccessor>();
        services.AddSingleton<IEventBusLogger, DefaultEventBusLogger>();
        services.AddSingleton<IEventBus, EventBusKafka>(sp => new EventBusKafka(sp));
    }

    private static void AddRabbitMqEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration objects
        services.Configure<RabbitMqConnectionSettings>(configuration.GetSection("RabbitMq:Connection"));
        services.Configure<RabbitMqEventBusConfig>(configuration.GetSection("RabbitMq:EventBus"));

        // Add event bus instances
        services.AddHttpContextAccessor();
        services.AddSingleton<ICurrentPrincipalAccessor, HttpContextCurrentPrincipalAccessor>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<ITraceAccesor, HttpContextTraceAccessor>();
        services.AddSingleton<IEventBusLogger, DefaultEventBusLogger>();
        services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();
        services.AddSingleton<IEventBus, EventBusRabbitMq>();
    }

    private static void AddEventHandlers(this IServiceCollection services, Assembly assembly)
    {
        var refType = typeof(IIntegrationEventHandler);
        var types = assembly.GetTypes()
            .Where(p => refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false });

        foreach (var type in types.ToList())
        {
            services.AddTransient(type);
        }
    }

    public static void UseEventBus(this IApplicationBuilder app, Assembly assembly)
    {
        var refType = typeof(IIntegrationEventHandler);
        var eventHandlerTypes = assembly.GetTypes()
            .Where(p => refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false }).ToList();

        if (eventHandlerTypes is not { Count: > 0 }) return;

        var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

        foreach (var eventHandlerType in eventHandlerTypes)
        {
            var eventType = eventHandlerType.GetInterfaces().First(x => x.IsGenericType).GenericTypeArguments[0];

            eventBus.Subscribe(eventType, eventHandlerType);
        }
    }
}