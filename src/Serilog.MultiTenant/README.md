# Serilog.MultiTenant

Proyecto para encapsular la extensión multi-tenant de Serilog.

## Objetivo

- Resolver nivel de logging por tenant usando `LogContext`.
- Soportar overrides dinámicos por tenant en runtime.
- Mantener fallback al nivel base definido por la aplicación host.
- No romper el pipeline de logging ante ausencia de `TenantId`.

## Componentes incluidos

- `ITenantLogLevelStore` + `InMemoryTenantLogLevelStore`.
- `ITenantLogLevelResolver` + `TenantLogLevelResolver`.
- `ITenantContextAccessor`/`ITenantContextSetter` + `AsyncLocalTenantContextAccessor`.
- `TenantAwareLogEventFilter` para filtrar por nivel efectivo por tenant.
- `TenantLogContextMiddleware` para publicar `TenantId` en `LogContext`.
- Extensiones para DI y configuración de `LoggerConfiguration`.

## Uso rápido

### Nivel base fijo en código

```csharp
using Serilog;
using Serilog.Events;
using Serilog.MultiTenant.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilogMultiTenant(
    baseLevel: LogEventLevel.Information,
    configureTenantLogContext: options =>
    {
        options.TenantIdResolver = context => context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
    });

builder.Host.UseSerilog((context, services, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .UseTenantAwareLevelFiltering(services);
});

WebApplication app = builder.Build();
app.UseTenantLogContext();
```

### Nivel base y overrides desde `IConfiguration`

El filtro también puede conectarse directamente a la configuración de la aplicación
(por ejemplo `appsettings.json`), sin necesidad de fijar el nivel base en código:

```json
{
  "Serilog": {
    "MultiTenant": {
      "BaseLevel": "Information",
      "TenantPropertyName": "TenantId",
      "TenantOverrides": {
        "tenant-a": "Verbose",
        "tenant-b": "Warning"
      }
    }
  }
}
```

```csharp
builder.Services.AddSerilogMultiTenant(
    builder.Configuration,
    configureTenantLogContext: options =>
    {
        options.TenantIdResolver = context => context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
    });

builder.Host.UseSerilog((context, services, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .UseTenantAwareLevelFiltering(services);
});
```

Por defecto se lee la sección `Serilog:MultiTenant`; puede cambiarse con el parámetro
`configSectionPath`. Para cualquier caso no cubierto por la configuración -o para
forzar un valor distinto- se puede pasar `configureTenantLogging`, que siempre se
aplica **después** del binding de configuración y tiene la última palabra:

```csharp
builder.Services.AddSerilogMultiTenant(
    builder.Configuration,
    configureTenantLogging: options =>
    {
        // Gana sobre lo que venga de appsettings.json.
        options.BaseLevel = LogEventLevel.Warning;
    });
```

## Overrides dinámicos

```csharp
using Serilog.Events;
using Serilog.MultiTenant.Abstractions;

ITenantLogLevelConfigurator configurator = app.Services.GetRequiredService<ITenantLogLevelConfigurator>();

configurator.SetLevel("tenant-a", LogEventLevel.Verbose);
configurator.SetLevel("tenant-b", LogEventLevel.Warning);
configurator.RemoveLevel("tenant-b");
```
