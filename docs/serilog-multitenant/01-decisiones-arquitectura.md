# Decisiones de arquitectura

## Decisión 1: captura global amplia

- El pipeline interno de Serilog se configura con `MinimumLevel.Verbose`.
- Motivo: permitir overrides de tenant más verbosos que el nivel base de negocio.
- **Límite conocido**: esta captura amplia solo cubre el `MinimumLevel`
  *default* del logger. Un `MinimumLevel:Override` por namespace (definido en
  código o en `appsettings.json`) sigue descartando eventos de ese namespace
  antes de que `TenantAwareLogEventFilter` los evalúe, y no se ve afectado
  por `MinimumLevel.Verbose()`. Además, si algo configura el `MinimumLevel`
  global *después* de `UseTenantAwareLevelFiltering`, puede volver a
  restringir la captura. Ver advertencia en el `README` de la librería.

## Decisión 2: nivel efectivo por tenant en filtro

- La emisión final de eventos se decide con un filtro tenant-aware por evento.
- Regla: `logEvent.Level >= effectiveLevel(tenant)`.

## Decisión 3: fallback al nivel base del host

- El nivel base funcional es el configurado en `Main`/`Program`.
- Si no hay tenant o no hay override, se aplica siempre ese base.

## Decisión 4: resolución de tenant desde LogContext

- Fuente primaria de `TenantId`: `LogContext`.
- Respaldo: `ITenantContextAccessor` (por ejemplo `AsyncLocal`).
- Motivo: eventos autocontenidos y mejor robustez en flujos asíncronos.

## Decisión 5: fail-safe

- El componente nunca debe lanzar excepción que rompa el pipeline de Serilog.
- Ante error interno, usar nivel base y continuar.

## Decisión 6: tenants dinámicos en runtime

- Overrides mantenidos en un store concurrente y mutable.
- Altas/bajas/cambios sin reinicio de la aplicación.
