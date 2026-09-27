using Serilog.Events;

namespace Serilog.MultiTenant.Options;

public sealed class TenantLoggingOptions
{
    public string TenantPropertyName { get; set; } = "TenantId";

    public LogEventLevel BaseLevel { get; set; } = LogEventLevel.Information;

    public Dictionary<string, LogEventLevel> TenantOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
