using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog.Context;
using Serilog.MultiTenant.Abstractions;
using Serilog.MultiTenant.Options;

namespace Serilog.MultiTenant.Middleware;

public sealed class TenantLogContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextSetter tenantContextSetter,
        IOptions<TenantLogContextMiddlewareOptions> optionsAccessor,
        IOptions<TenantLoggingOptions> loggingOptionsAccessor)
    {
        TenantLogContextMiddlewareOptions options = optionsAccessor.Value;
        string? tenantId = options.TenantIdResolver(context);
        string tenantPropertyName = string.IsNullOrWhiteSpace(loggingOptionsAccessor.Value.TenantPropertyName)
            ? "TenantId"
            : loggingOptionsAccessor.Value.TenantPropertyName;

        using IDisposable tenantScope = tenantContextSetter.BeginScope(tenantId);
        using IDisposable logScope = PushTenantPropertyIfPresent(tenantId, tenantPropertyName);

        await next(context);
    }

    private static IDisposable PushTenantPropertyIfPresent(string? tenantId, string tenantPropertyName)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return NoopDisposable.Instance;
        }

        return LogContext.PushProperty(tenantPropertyName, tenantId);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
