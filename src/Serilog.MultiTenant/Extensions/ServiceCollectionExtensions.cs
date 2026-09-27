using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog.Events;
using Serilog.MultiTenant.Abstractions;
using Serilog.MultiTenant.Configuration;
using Serilog.MultiTenant.Context;
using Serilog.MultiTenant.Options;

namespace Serilog.MultiTenant.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the multi-tenant Serilog filter with a fixed base level defined in code.
    /// </summary>
    public static IServiceCollection AddSerilogMultiTenant(
        this IServiceCollection services,
        LogEventLevel baseLevel,
        Action<TenantLoggingOptions>? configureTenantLogging = null,
        Action<TenantLogContextMiddlewareOptions>? configureTenantLogContext = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        AddCoreServices(services, configureTenantLogContext);

        services.AddSingleton<IBaseLogLevelProvider>(_ => new FixedBaseLogLevelProvider(baseLevel));
        services.AddSingleton<ITenantLogLevelStore, InMemoryTenantLogLevelStore>();

        if (configureTenantLogging is not null)
        {
            services.Configure(configureTenantLogging);
        }

        return services;
    }

    /// <summary>
    /// Registers the multi-tenant Serilog filter and binds its base level, tenant property name and
    /// initial tenant overrides from <paramref name="configuration"/> (e.g. "Serilog:MultiTenant" in
    /// appsettings.json). For any setting the configuration does not cover -- or to override it -- pass
    /// <paramref name="configureTenantLogging"/>, which is always applied after the configuration binding.
    /// </summary>
    public static IServiceCollection AddSerilogMultiTenant(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionPath = "Serilog:MultiTenant",
        Action<TenantLoggingOptions>? configureTenantLogging = null,
        Action<TenantLogContextMiddlewareOptions>? configureTenantLogContext = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(configSectionPath);

        AddCoreServices(services, configureTenantLogContext);

        services.Configure<TenantLoggingOptions>(configuration.GetSection(configSectionPath));

        if (configureTenantLogging is not null)
        {
            services.PostConfigure(configureTenantLogging);
        }

        services.AddSingleton<IBaseLogLevelProvider, OptionsBaseLogLevelProvider>();
        services.AddSingleton<ITenantLogLevelStore>(provider =>
        {
            TenantLoggingOptions options = provider.GetRequiredService<IOptions<TenantLoggingOptions>>().Value;

            InMemoryTenantLogLevelStore store = new();
            foreach ((string tenantId, LogEventLevel level) in options.TenantOverrides)
            {
                store.Set(tenantId, level);
            }

            return store;
        });

        return services;
    }

    private static void AddCoreServices(
        IServiceCollection services,
        Action<TenantLogContextMiddlewareOptions>? configureTenantLogContext)
    {
        services.AddOptions<TenantLoggingOptions>();
        services.AddOptions<TenantLogContextMiddlewareOptions>();

        services.AddSingleton<ITenantLogLevelResolver, TenantLogLevelResolver>();
        services.AddSingleton<ITenantLogLevelConfigurator, TenantLogLevelConfigurator>();
        services.AddSingleton<AsyncLocalTenantContextAccessor>();
        services.AddSingleton<ITenantContextAccessor>(provider => provider.GetRequiredService<AsyncLocalTenantContextAccessor>());
        services.AddSingleton<ITenantContextSetter>(provider => provider.GetRequiredService<AsyncLocalTenantContextAccessor>());

        if (configureTenantLogContext is not null)
        {
            services.Configure(configureTenantLogContext);
        }
    }
}
