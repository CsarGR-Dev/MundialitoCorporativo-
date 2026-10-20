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

- [x] Logs en formato JSON con propiedades (`EquipoId`, `ElapsedMs`, `TraceId`, `CorrelationId`).
- [x] Ningún log contiene cadenas de conexión ni cuerpos completos de request.
- [x] Cada respuesta incluye `X-Correlation-Id` y `X-Trace-Id`.
- [x] Cada request genera un log con su duración.
- [x] Al crear equipo y registrar resultado se guarda un registro en `EventosDominio` y se emite un log.
- [x] (Valorable) Trazas visibles en Jaeger: servicio `jaeger` en docker-compose (ítem 12), UI en http://localhost:16686, servicio `mundialito-api`.

## Implementación real

### Ajustes respecto al diseño inicial

| Diseño inicial | Implementado | Motivo |
|---|---|---|
| `ICorrelationIdAccessor` como servicio | El CorrelationId viaja como *baggage* de la traza (`Activity`) y en `HttpContext.Items` | Application e Infrastructure lo leen sin depender de HTTP ni de un servicio con estado; también funciona en pruebas sin la API. |
| `UseSerilogRequestLogging` o middleware propio | Middleware propio `RequestTimingMiddleware` | Control del mensaje en español, de la propiedad `ElapsedMs` y del nivel por resultado y por request lento. |
| Pipeline `ExceptionHandling → CorrelationId → RequestTiming → …` | `CorrelationId → RequestTiming → ExceptionHandler → StatusCodePages → Routing → Idempotency → Controllers` | La trazabilidad va por fuera del manejador de excepciones para que un 500 también lleve sus headers, su duración y el `correlationId` en el ProblemDetails. |
| Exportación OTLP siempre activa | Activa solo si `Observability:OtlpEndpoint` tiene valor | Sin Jaeger disponible la API no intenta exportar; en Docker se configura el endpoint. |
| Nombres en inglés del ejemplo (`TeamId`, `HomeGoals`) | Propiedades en español (`EquipoId`, `GolesLocal`) | Convención del proyecto: los modelos y sus datos en español. |

### Logging estructurado (Serilog sobre `ILogger<T>`)

- `Api/Observability/ObservabilityExtensions.cs`: `AddSerilog` reemplaza el proveedor de logs. Las capas usan solo `ILogger<T>`; Serilog se conoce únicamente en Api.
- Consola: JSON (`RenderedCompactJsonFormatter`) por defecto; texto legible en desarrollo (`Observability:ConsoleFormat = Text`). Archivo diario en JSON (`logs/mundialito-AAAAMMDD.json`, 7 días).
- Enriquecimiento: `Application`, `Environment`, `MachineName`, `TraceId`, `SpanId`, `CorrelationId` y, dentro de un request, `RequestPath` y `ActionName`.
- Niveles mínimos en la sección `Serilog` de `appsettings.json` (`Microsoft` y `System` en Warning). En desarrollo, `MundialitoCorporativo` en Debug.
- Todos los mensajes usan plantillas con `[LoggerMessage]` (código generado, sin interpolación).

| Nivel | Dónde |
|---|---|
| Debug | Query ejecutada y su duración; respuesta guardada por idempotencia. |
| Information | Request 2xx; Command ejecutado; evento de dominio registrado; equipo creado; resultado registrado; repetición idempotente. |
| Warning | Request 4xx; Result fallido (con `ErrorType` y `ErrorCode`); request lento (> 500 ms); clave de idempotencia reutilizada o en proceso; conflicto de concurrencia o restricción de base de datos. |
| Error | Excepción no controlada (una sola vez, en `GlobalExceptionHandler`); request 5xx; handler de evento que falla. |

Datos que **no** se registran: cuerpos de petición, query string, headers, contenido de los Commands y cadenas de conexión. Los Commands y Queries se registran solo por nombre, resultado y duración.

### Trazabilidad

- `CorrelationIdMiddleware`: acepta `X-Correlation-Id` de 1 a 64 caracteres ASCII visibles (el largo de la columna `EventosDominio.CorrelationId`); si falta o no es válido, genera un GUID. Devuelve `X-Correlation-Id` y `X-Trace-Id` en todas las respuestas, incluidas las de error.
- TraceId: el de W3C Trace Context que crea ASP.NET Core. Si el cliente envía `traceparent`, la API continúa esa traza.
- Los dos valores se agregan como propiedades a todos los logs del request y aparecen en todo ProblemDetails (`traceId`, `correlationId`).
- `RequestTimingMiddleware`: `HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {ElapsedMs} ms`, con nivel según el resultado y Warning si supera `Observability:SlowRequestThreshold`.
- `ObservabilityBehavior`, en el pipeline de MediatR, abre un span por Command o Query (`ActivitySource "MundialitoCorporativo.Application"`) con el nombre, el tipo y, si falla, el `ErrorCode`.
- OpenTelemetry (valorable): instrumentación de ASP.NET Core y SqlClient (cubre EF Core y Dapper), spans propios de Application y exportación OTLP hacia Jaeger cuando hay endpoint configurado.

### Eventos de dominio

| Paso | Implementación |
|---|---|
| Modelado | `EquipoCreatedDomainEvent` y `PartidoResultRegisteredDomainEvent` (obligatorios), además de `JugadorRegisteredDomainEvent` y `PartidoScheduledDomainEvent`. Los agregados los generan en sus métodos de dominio. |
| Registro | `UnitOfWork.CommitAsync` los guarda en `EventosDominio` en la misma transacción que los datos, con `TraceId` y `CorrelationId`. |
| Despacho | Tras el commit, `IDomainEventDispatcher` (Application) entrega cada evento a sus `IDomainEventHandler<T>`. Sin bus externo. |
| Log | El Unit of Work registra cada evento (`DomainEventId`, `DomainEventType`, agregado) y los handlers registran el hecho de negocio: `Equipo creado: {EquipoId} {NombreEquipo} ({Abreviatura})` y `Resultado registrado en el partido {PartidoId} del torneo {TorneoId}: …`. |
| Procesado | Si todos los handlers terminan bien se guarda `FechaProcesado`. Si uno falla, la operación no se revierte (los datos ya están confirmados), el fallo se registra con nivel Error y el evento queda pendiente (`IX_EventosDominio_Pendientes`). |
| Trazabilidad | El mismo `TraceId` une el request, los logs, el span del Command y la fila de `EventosDominio`. |

Los handlers se ejecutan con `CancellationToken.None`: si el cliente cancela la petición después del commit, el evento igual se procesa.

### Pruebas

- Unitarias:
  - `CorrelationIdMiddlewareTests`: se conserva un valor válido, se genera uno nuevo si falta o no es válido, y TraceId y CorrelationId llegan a las capas internas y a la respuesta.
  - `RequestTimingMiddlewareTests`: el nivel según el código y el request lento; no registra la query string.
  - `MediatorPipelineTests`: el log del Command sin su contenido, el Warning con `ErrorCode` y un span por Command o Query.
  - `DomainEventDispatcherTests`: los handlers de los eventos obligatorios y sus propiedades estructuradas.
  - `UnitOfWorkTests`: el despacho después del commit, `FechaProcesado`, un handler que falla y el TraceId y CorrelationId guardados.
- Integración (`ObservabilitySqlServerTests`, por HTTP contra SQL Server):
  - headers en la respuesta y continuidad de un `traceparent` entrante;
  - `traceId` y `correlationId` en ProblemDetails;
  - la fila de `EventosDominio` con el TraceId y el CorrelationId del POST;
  - 500 sin detalles internos.
