using Microsoft.AspNetCore.Http;

namespace Serilog.MultiTenant.Options;

public sealed class TenantLogContextMiddlewareOptions
{
    public Func<HttpContext, string?> TenantIdResolver { get; set; } = static _ => null;
}
