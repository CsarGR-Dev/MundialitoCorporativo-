# Ítem 7 – Observabilidad y Trazabilidad (Obligatorio)

## Requerimiento

**Logging estructurado**
- Uso de ILogger o Serilog.
- Logs estructurados (no solo strings planos).
- No loggear datos sensibles.
- Niveles correctos: Information, Warning, Error, Debug.

**Trazabilidad (Tracing)**
- CorrelationId por request.
- Middleware que agregue TraceId.
- Registro de duración de requests.
- Logs enlazados al TraceId.

**Opcional valorable**: OpenTelemetry, exportación a Jaeger, Zipkin o similar.

**Eventos de Dominio**
- Al menos: evento cuando se registra resultado y evento cuando se crea equipo.
- Deben ser registrados, loggeados y mantener trazabilidad.
- No es obligatorio Event Bus externo, pero sí buena modelación.

## 1. Logging estructurado

### Configuración (Serilog sobre `ILogger<T>`)

- `Serilog.AspNetCore` + `UseSerilog()` en `Program.cs`; el código de aplicación usa `ILogger<T>` (no depende de Serilog).
- Sinks: Console con formato JSON compacto (`CompactJsonFormatter`), archivo rotativo diario, y opcionalmente Seq.
- Enrichers: `FromLogContext`, `WithMachineName`, `WithEnvironmentName`, `WithProperty("Application", "Mundialito.Api")`.
- Niveles por defecto: `Information`; `Microsoft.AspNetCore` y `Microsoft.EntityFrameworkCore` en `Warning`; `Debug` habilitable por `appsettings.Development.json`.

### Plantillas estructuradas (nunca interpolación)

```csharp
logger.LogInformation("Equipo {TeamId} creado con nombre {TeamName}", team.Id, team.Name);
```

### Uso de niveles

| Nivel | Cuándo |
|---|---|
| Debug | Entrada/salida de handlers, parámetros de consulta, SQL generado en desarrollo. |
| Information | Request completado, comando ejecutado con éxito, evento de dominio emitido, seed ejecutado. |
| Warning | Resultado de negocio fallido (400/404/409), replay de idempotencia, key reutilizada, request lento (> 500 ms). |
| Error | Excepción no controlada, fallo de base de datos, rollback de transacción. |

### Datos sensibles

- No se registran cuerpos completos de request ni headers como `Authorization` o `Cookie`.
- Se usa `[LogMasked]` / destructuring policy o se registran solo identificadores.
- Las cadenas de conexión nunca se registran.
- Datos personales de jugadores (nombre completo) no se registran; se registra `PlayerId`.

## 2. Trazabilidad

### `CorrelationIdMiddleware`

1. Lee el header `X-Correlation-Id`; si no viene, genera un GUID.
2. Lo guarda en `HttpContext.Items` y en un servicio `ICorrelationIdAccessor`.
3. Lo devuelve en el header de respuesta `X-Correlation-Id`.
4. Abre un `LogContext.PushProperty("CorrelationId", ...)` para todo el request.

### TraceId

- Se toma de `Activity.Current.TraceId` (W3C Trace Context; respeta `traceparent` entrante).
- Se agrega al `LogContext` como `TraceId` y `SpanId`.
- Se devuelve en el header `X-Trace-Id` y dentro de ProblemDetails (`traceId`).

### `RequestTimingMiddleware`

- `Stopwatch` alrededor de `next()`.
- Log al finalizar: `HTTP {Method} {Path} respondió {StatusCode} en {ElapsedMs} ms`.
- Nivel según resultado: 2xx Information, 4xx Warning, 5xx Error; > umbral configurable → Warning.
- Alternativa equivalente: `UseSerilogRequestLogging` con enriquecimiento de `CorrelationId`.

### Orden del pipeline

```
ExceptionHandling → CorrelationId → RequestTiming → Routing → IdempotencyFilter → Controller
```

### OpenTelemetry (valorable)

- `AddOpenTelemetry().WithTracing(...)` con instrumentación de ASP.NET Core, HttpClient y SqlClient.
- `ActivitySource("Mundialito.Application")` para spans propios por Command/Query.
- Exportador OTLP hacia **Jaeger** (servicio `jaeger` en docker-compose, UI en puerto 16686).
- Métricas opcionales: `WithMetrics` con contadores de requests y duración.

## 3. Eventos de dominio

### Modelado

```csharp
public interface IDomainEvent { Guid EventId { get; } DateTime OccurredOnUtc { get; } }

public sealed record EquipoCreatedDomainEvent(Guid TeamId, string Name) : IDomainEvent;
public sealed record PartidoResultRegisteredDomainEvent(
    Guid MatchId, Guid HomeTeamId, Guid AwayTeamId, int HomeGoals, int AwayGoals) : IDomainEvent;
```

- Las entidades acumulan eventos (`Raise(...)`) dentro de sus métodos de dominio (`Equipo.Create`, `Partido.RegisterResult`).
- `IDomainEventHandler<TEvent>` en Application; handlers que registran el evento en log.

### Registro, log y trazabilidad

| Paso | Componente |
|---|---|
| Registro | `UnitOfWork.CommitAsync` guarda cada evento en `EventosDominio` (TipoEvento, TipoAgregado, AgregadoId, DatosEvento JSON, FechaOcurrencia, TraceId, CorrelationId) dentro de la misma transacción. |
| Despacho | Tras commit exitoso, `IDomainEventDispatcher` invoca los handlers en proceso. |
| Log | Cada handler registra: `Evento {EventType} {EventId} para agregado {AggregateId}` con nivel Information. |
| Trazabilidad | El TraceId y CorrelationId del request quedan en la tabla y en el log, permitiendo enlazar request → comando → evento. |

Opcional: endpoint `GET /api/eventos-dominio` (paginado, Dapper) para consultar los eventos registrados.

## Criterios de aceptación

- [ ] Logs en formato JSON con propiedades (`TeamId`, `ElapsedMs`, `TraceId`, `CorrelationId`).
- [ ] Ningún log contiene cadenas de conexión ni cuerpos completos de request.
- [ ] Cada respuesta incluye `X-Correlation-Id` y `X-Trace-Id`.
- [ ] Cada request genera un log con su duración.
- [ ] Al crear equipo y registrar resultado se guarda un registro en `EventosDominio` y se emite un log.
- [ ] (Valorable) Trazas visibles en Jaeger.
