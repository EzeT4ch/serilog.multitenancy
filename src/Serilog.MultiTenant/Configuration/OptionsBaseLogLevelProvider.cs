using Microsoft.Extensions.Options;
using Serilog.Events;
using Serilog.MultiTenant.Abstractions;
using Serilog.MultiTenant.Options;

namespace Serilog.MultiTenant.Configuration;

public sealed class OptionsBaseLogLevelProvider(IOptionsMonitor<TenantLoggingOptions> optionsMonitor) : IBaseLogLevelProvider
{
    public LogEventLevel BaseLevel => optionsMonitor.CurrentValue.BaseLevel;
}
