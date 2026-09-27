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

## Advertencia: el nivel mínimo global de Serilog puede tapar al filtro

Serilog descarta un `LogEvent` por debajo de su `MinimumLevel` (y de cualquier
`MinimumLevel:Override` por namespace) **antes** de que se evalúe ningún
`Filter`, incluido `TenantAwareLogEventFilter`. Si ese nivel global es más
restrictivo que lo que un tenant necesita, el filtro nunca llega a ver esos
eventos y el override del tenant no tiene efecto alguno.

Por eso `UseTenantAwareLevelFiltering(services)` fija internamente
`MinimumLevel.Verbose()`: así el pipeline captura todo y es el filtro
tenant-aware quien decide, por evento, qué se descarta según el nivel
efectivo de cada tenant. Tené en cuenta:

- **Orden de configuración**: llamá `UseTenantAwareLevelFiltering` *después*
  de `ReadFrom.Configuration` (como en los ejemplos anteriores). Si se llama
  antes, un `Serilog:MinimumLevel` definido en `appsettings.json` puede
  sobrescribir el `Verbose()` y volver a filtrar eventos antes de que el
  filtro los evalúe.
- **`MinimumLevel:Override` por namespace**: los overrides por namespace
  (por ejemplo `"Serilog:MinimumLevel:Override:Microsoft": "Warning"`) son
  independientes del nivel global y **no** se ven afectados por el
  `MinimumLevel.Verbose()` de `UseTenantAwareLevelFiltering`. Si un tenant
  necesita ver eventos más detallados que ese override, los eventos se
  seguirán descartando antes de llegar al filtro. Revisá que no existan
  overrides por namespace más estrictos que el nivel más verboso que
  cualquier tenant pueda necesitar.
- **Configuración manual**: si en lugar de `UseTenantAwareLevelFiltering`
  llamás directamente a `.Filter.ByTenantLevel(...)`, sos responsable de
  fijar vos mismo un `MinimumLevel` (global y por override) igual o más
  verboso que el nivel más detallado que cualquier tenant pueda requerir.

## Overrides dinámicos

```csharp
using Serilog.Events;
using Serilog.MultiTenant.Abstractions;

ITenantLogLevelConfigurator configurator = app.Services.GetRequiredService<ITenantLogLevelConfigurator>();

configurator.SetLevel("tenant-a", LogEventLevel.Verbose);
configurator.SetLevel("tenant-b", LogEventLevel.Warning);
configurator.RemoveLevel("tenant-b");
```
