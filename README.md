# Mundialito Corporativo

Sistema de gestión de un torneo de fútbol corporativo: equipos, integrantes, calendario de partidos, resultados, tabla de posiciones y goleadores.

API en **.NET 10** con **Clean Architecture**, **CQRS** (EF Core para escritura, Dapper para lectura), **Unit of Work**, **Result Pattern**, REST idempotente y observabilidad; base de datos **SQL Server 2022**.

> **Estado:** en desarrollo. Ver [Estado por ítem](#estado-por-ítem) y la [bitácora de cambios](Documentacion/Bitacora.md).

---

## Estructura

```
MundialitoCorporativo/
├── src/
│   ├── MundialitoCorporativo.Domain          Modelos, reglas de negocio, Result Pattern, eventos de dominio
│   ├── MundialitoCorporativo.Application     CQRS: despachador, Commands, Queries, validaciones y contratos
│   ├── MundialitoCorporativo.Infrastructure  EF Core, repositorios, Unit of Work y consultas Dapper
│   └── MundialitoCorporativo.Api             Controladores REST, idempotencia, observabilidad, errores HTTP y composition root
├── tests/
│   ├── MundialitoCorporativo.UnitTests         Arquitectura, dominio, CQRS, Unit of Work, idempotencia, mapeo HTTP y observabilidad
│   └── MundialitoCorporativo.IntegrationTests  CQRS, idempotencia, trazabilidad y API por HTTP contra SQL Server
├── Db/              Scripts SQL (estructura, vistas y consultas de referencia)
├── Analisis/        Análisis del requerimiento, un documento por ítem
└── Documentacion/   Bitácora de cambios
```

Reglas de dependencia (verificadas con pruebas de arquitectura): Domain no depende de ninguna capa; Application depende solo de Domain; Infrastructure implementa los contratos de Application; Api solo orquesta.

## Requisitos

- .NET SDK 10
- SQL Server 2019 o superior (desarrollo: SQL Server Express con autenticación Windows)

## Base de datos

Ejecutar en SSMS, en orden:

1. `Db/001_DDL_CrearBaseDatos.sql` — base `Mundialito`: tablas, relaciones, restricciones, índices y catálogo de posiciones de juego.
2. `Db/002_VIEW_TablaPosicionesGoleadores.sql` — vistas `vw_TablaPosiciones` y `vw_Goleadores`.

La cadena de conexión de desarrollo está en `src/MundialitoCorporativo.Api/appsettings.Development.json`:

```
Server=.\SQLEXPRESS;Database=Mundialito;Trusted_Connection=True;TrustServerCertificate=True
```

Diseño y diccionario de datos: [Analisis/16_Analisis_Base_Datos.md](Analisis/16_Analisis_Base_Datos.md).

## Compilar, probar y ejecutar

```bash
dotnet build
dotnet test
dotnet run --project src/MundialitoCorporativo.Api      # http://localhost:5000
```

Las pruebas de integración (incluida la API completa con `WebApplicationFactory`) crean una base temporal ejecutando `Db/001` y la eliminan al terminar. Usan la instancia local por defecto; otra instancia se indica con la variable de entorno `MUNDIALITO_TEST_SQLSERVER`. Si no hay un SQL Server disponible, se omiten.

## API REST

Base: `http://localhost:5000/api`. En desarrollo, el documento OpenAPI está en `/openapi/v1.json`.

| Recurso | Operaciones |
|---|---|
| `/torneos` | GET (paginado), POST · `/{id}`: GET, PUT, PATCH, DELETE |
| `/torneos/{id}/equipos` | GET, POST (inscribir) · `/{equipoId}`: DELETE (retirar) |
| `/torneos/{id}/partidos`, `/tabla-posiciones`, `/goleadores` | GET (paginado) |
| `/equipos` | GET (paginado), POST · `/{id}`: GET, PUT, PATCH, DELETE |
| `/equipos/{id}/jugadores` | GET (integrantes), POST (registrar integrante) |
| `/jugadores` | GET (paginado) · `/{id}`: GET, PUT, PATCH, DELETE |
| `/partidos` | GET (paginado), POST · `/{id}`: GET, PUT, PATCH, DELETE |
| `/partidos/{id}/resultado` | POST (registrar resultado y goleadores) |
| `/posiciones-juego` | GET |

- **Paginación y orden** en todos los GET de colección: `pageNumber`, `pageSize` (1–100), `sortBy`, `sortDirection` (`asc`/`desc`) y filtros por entidad. Respuesta `{ data, pageNumber, pageSize, totalRecords, totalPages }`.
- **Idempotencia**: todo POST exige el encabezado `Idempotency-Key`. Repetir la petición con la misma clave devuelve la respuesta original (`Idempotent-Replayed: true`) sin duplicar registros; la misma clave con otro cuerpo responde 409.
- **Códigos**: 200, 201 (con `Location`), 204, 400, 404, 409 y 500. Los errores usan ProblemDetails con `errorCode`, `traceId` y `correlationId`.
- **PATCH** acepta `application/merge-patch+json` o `application/json`; solo cambia los campos enviados.

Ejemplo:

```bash
curl -X POST http://localhost:5000/api/equipos \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: 6f1c2a54-3d0e-4b7a-9c61-2f8e5d4a1b90" \
  -d '{"nombre":"Tecnología FC","abreviatura":"TEC","departamento":"Sistemas"}'
```

Detalle de verbos, códigos e idempotencia: [Analisis/06_REST_Idempotencia.md](Analisis/06_REST_Idempotencia.md).

## Observabilidad

- **Logs estructurados** con Serilog sobre `ILogger<T>`: consola en JSON (texto legible en desarrollo) y archivo diario en JSON en `logs/` dentro de la carpeta desde la que se ejecuta la API. No se registran cuerpos de petición, query string ni cadenas de conexión.
- **Trazabilidad por request**: la API acepta `X-Correlation-Id` (o genera uno) y devuelve `X-Correlation-Id` y `X-Trace-Id` (W3C Trace Context). Ambos valores están en cada log del request y en todo ProblemDetails.
- **Duración**: un log por request, `HTTP GET /api/equipos respondió 200 en 12.3 ms`, con nivel Information, Warning (4xx o más de 500 ms) o Error (5xx).
- **Eventos de dominio**: al crear un equipo y al registrar un resultado, el evento se guarda en `EventosDominio` con el TraceId y el CorrelationId del request, se despacha a su handler y queda en el log. Consulta en `Db/013_QRY_EventosDominio.sql`.
- **OpenTelemetry**: con `Observability:OtlpEndpoint` configurado (por ejemplo, `http://localhost:4317` de Jaeger), se exportan las trazas de ASP.NET Core, SQL Server y los Commands/Queries.

Detalle: [Analisis/07_Observabilidad_Trazabilidad.md](Analisis/07_Observabilidad_Trazabilidad.md).

## Convención de nombres

Los **modelos** están en español con el mismo nombre que las tablas y columnas (`Equipo.Nombre`, `Partido.EquipoLocalId`, `EstadoPartido.Jugado`). **Lo técnico** sigue la convención de .NET en inglés (`Entity`, `AggregateRoot`, `Result`, `Create`, `EquipoErrors`, `TablaPosicionesCalculator`). Detalle: [Analisis/17_Convencion_Nombres.md](Analisis/17_Convencion_Nombres.md).

## Estado por ítem

| # | Ítem | Estado |
|---|---|---|
| 2 | Clean Architecture | Implementado: 4 capas y pruebas de arquitectura |
| 3 | CQRS | Implementado: despachador, Commands (EF Core + Unit of Work) y Queries (Dapper) |
| 4 | Unit of Work | Implementado y probado |
| 5 | Result Pattern | Implementado: Result en Domain y mapeo a 200/201/204/400/404/409 con ProblemDetails |
| 6 | REST + idempotencia | Implementado: controladores REST e `Idempotency-Key` persistida en `RegistrosIdempotencia` |
| 7 | Observabilidad | Implementado: Serilog con logs JSON, CorrelationId y TraceId por request, duración de requests, eventos de dominio registrados, despachados y en el log; OpenTelemetry con exportación OTLP (Jaeger con Docker, pendiente) |
| 8 | Requisitos funcionales | Implementado y expuesto por HTTP: torneos, equipos, jugadores por equipo, partidos, resultados, tabla de posiciones y goleadores |
| 9 | Seed de datos | Pendiente |
| 10 | Filtros y paginación | Implementado en todos los GET de colección (paginación en SQL, sortBy con lista blanca) |
| 11 | Frontend Next.js | Pendiente |
| 12 | Dockerización | Pendiente |
| 13 | Diagrama de arquitectura | Pendiente |
| 14 | Colección de Postman | Pendiente |
| 15 | Pruebas | Todo el mínimo cubierto (tabla, puntos, diferencia de gol, Result Pattern, idempotencia, Unit of Work) más integración por HTTP y observabilidad: 218 pruebas |

El análisis de cada ítem está en [Analisis/](Analisis/).
