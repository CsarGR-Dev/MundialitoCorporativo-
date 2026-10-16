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
        D[Dispatcher]
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

## Criterios de aceptación

- [ ] Los 11 elementos exigidos aparecen en el diagrama.
- [ ] El diagrama está en el repositorio y visible desde el README.
- [ ] Coincide con la implementación real (nombres de clases y servicios).
