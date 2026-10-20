# Ítem 13 – Diagrama de Arquitectura (Obligatorio)

## Requerimiento

Debe representar: capas, CQRS, EF Core (write), Dapper (read), UnitOfWork, idempotency storage, eventos, logs, trace flow, frontend y Docker.

## Entregable

- Archivo fuente editable en `docs/arquitectura/` (Mermaid `.md` renderizable en GitHub y/o draw.io `.drawio`).
- Exportación a imagen (`.png` o `.svg`) incrustada en el README.
- Se recomiendan tres vistas: **contenedores (Docker)**, **componentes por capa (CQRS)** y **secuencia de un request (trace flow)**.

## Vista 1 – Contenedores (Docker)

```mermaid
flowchart LR
    U[Usuario / Postman] --> FE
    subgraph Docker Compose
        FE[frontend<br/>Next.js :3000]
        API[api<br/>ASP.NET Core :8080]
        DB[(db<br/>SQL Server :1433)]
        J[jaeger<br/>:16686]
    end
    FE -- HTTP/JSON --> API
    U -- HTTP/JSON --> API
    API -- EF Core / Dapper --> DB
    API -- OTLP traces --> J
```

## Vista 2 – Capas y CQRS

```mermaid
flowchart TB
    subgraph Api
        MW[Middlewares<br/>CorrelationId · TraceId · Timing · Exceptions]
        IF[IdempotencyFilter]
        C[Controllers]
        RX[Result → HTTP]
    end
    subgraph Application
        D[MediatR]
        CMD[Command Handlers]
        QRY[Query Handlers]
        VAL[Validators]
        IUOW[[IUnitOfWork]]
        IREPO[[I*Repository]]
        IRead[[IDbConnectionFactory]]
        IIdem[[IIdempotencyStore]]
    end
    subgraph Domain
        ENT[Entidades · Agregados]
        RES[Result / Error]
        EV[Domain Events]
        CALC[TablaPosicionesCalculator]
    end
    subgraph Infrastructure
        UOW[UnitOfWork]
        CTX[DbContext EF Core]
        REPO[Repositorios]
        DAP[Consultas Dapper]
        IST[Idempotency Store]
        DISP[Event Dispatcher + EventosDominio]
        SEED[Seeder]
        LOG[Serilog · OpenTelemetry]
    end
    DB[(SQL Server)]

    MW --> IF --> C --> D
    D -->|Commands - escritura| CMD
    D -->|Queries - lectura| QRY
    CMD --> IREPO & IUOW
    QRY --> IRead
    CMD --> ENT
    IUOW -.implementa.- UOW
    IREPO -.implementa.- REPO
    IRead -.implementa.- DAP
    IIdem -.implementa.- IST
    UOW --> CTX --> DB
    REPO --> CTX
    DAP --> DB
    IST --> DB
    UOW --> DISP --> DB
    C --> RX
```

## Vista 3 – Secuencia: registrar resultado (trace flow)

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant MW as Middlewares
    participant IF as IdempotencyFilter
    participant H as RegisterMatchResultHandler
    participant UOW as UnitOfWork
    participant DB as SQL Server
    participant L as Logs / Jaeger

    FE->>MW: POST /api/partidos/{id}/resultado + Idempotency-Key
    MW->>MW: CorrelationId + TraceId, inicia cronómetro
    MW->>IF: request
    IF->>DB: ¿existe key?
    alt key ya completada
        IF-->>FE: respuesta original (replay)
    else key nueva
        IF->>DB: registra key EnProceso
        IF->>H: comando
        H->>DB: carga Partido y Jugadores (EF Core)
        H->>H: partido.RegisterResult() → Result + evento
        H->>UOW: CommitAsync()
        UOW->>DB: BEGIN TRAN · UPDATE Partidos · INSERT Goles · INSERT EventosDominio · COMMIT
        UOW->>L: log MatchResultRegistered {TraceId}
        H-->>IF: Result.Success
        IF->>DB: guarda respuesta (Completed)
        IF-->>FE: 201 Created
    end
    MW->>L: log duración {ElapsedMs} {TraceId}
```

## Checklist de elementos exigidos

| Elemento | Vista |
|---|---|
| Capas | 2 |
| CQRS | 2 |
| EF Core (write) | 2, 3 |
| Dapper (read) | 2 |
| UnitOfWork | 2, 3 |
| Idempotency storage | 2, 3 |
| Eventos | 2, 3 |
| Logs | 2, 3 |
| Trace flow | 3 |
| Frontend | 1, 3 |
| Docker | 1 |

## Implementación real

El diagrama está en [`Arquitectura/README.md`](../Arquitectura/README.md), con las imágenes exportadas en `Arquitectura/img/`. Las vistas de arriba fueron el borrador; la versión final se centra en el sistema del Mundialito:

- **0. Vista general:** frontend con sus secciones, API con sus recursos, SQL Server con sus tablas y Jaeger, dentro de Docker Compose.
- **A. Modelo de datos:** `Torneos`, `TorneoEquipos`, `Equipos`, `Jugadores`, `PosicionesJuego`, `Partidos` y `Goles` con sus relaciones.
- **B. Ciclo de un torneo:** de crear el torneo a cerrarlo, con el endpoint de cada paso.
- **1 a 11:** un diagrama por cada elemento exigido, en el orden del requerimiento, con los nombres reales (19 Commands y 12 Queries, `RegisterPartidoResultCommandHandler`, los cuatro eventos de dominio, las páginas del frontend) y ejemplos de los datos iniciales.

### Ajustes respecto al diseño inicial

| Diseño inicial | Implementado | Motivo |
|---|---|---|
| Carpeta `docs/arquitectura/` | `Arquitectura/` en la raíz | Mismo estilo que `Analisis/` y `Postman/`. |
| Tres vistas grandes | Vista general, modelo de datos, ciclo del torneo y un diagrama por elemento | Cada diagrama se explica en pocos segundos, se corresponde uno a uno con la lista del requerimiento y muestra el contenido del Mundialito, no solo la arquitectura genérica. |
| `IdempotencyFilter` | `IdempotencyMiddleware` | Es un middleware: va después del enrutamiento y antes de los controladores. |
| `RegisterMatchResultHandler` | `RegisterPartidoResultCommandHandler` | Nombre real. |
| Fuente draw.io opcional | Solo Mermaid (fuente) y PNG exportado | GitHub dibuja Mermaid sin herramientas extra y el diagrama se versiona como texto. |

## Criterios de aceptación

- [x] Los 11 elementos exigidos aparecen en el diagrama (un diagrama por elemento en `Arquitectura/README.md`).
- [x] El diagrama está en el repositorio y visible desde el README.
- [x] Coincide con la implementación real (nombres de clases y servicios).
