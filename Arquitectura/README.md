# Diagrama de arquitectura

Arquitectura del Mundialito Corporativo: el sistema que gestiona torneos de fútbol entre departamentos, con sus equipos, integrantes, calendario de partidos, resultados, tabla de posiciones y goleadores.

Primero se presenta el sistema (vista general, modelo de datos y ciclo de un torneo) y después un diagrama por cada elemento de la arquitectura, siempre con los nombres reales de las clases, tablas y endpoints y con ejemplos de los datos iniciales. GitHub dibuja los diagramas Mermaid directamente; cada uno también está exportado como imagen en [`img/`](img/).

| # | Diagrama | Imagen |
|---|---|---|
| 0 | [Vista general](#0-vista-general) | [PNG](img/00_vista_general.png) |
| A | [Modelo de datos del Mundialito](#a-modelo-de-datos-del-mundialito) | [PNG](img/00_modelo_datos.png) |
| B | [Ciclo de un torneo](#b-ciclo-de-un-torneo) | — |
| 1 | [Capas](#1-capas) | [PNG](img/01_capas.png) |
| 2 | [CQRS](#2-cqrs) | [PNG](img/02_cqrs.png) |
| 3 | [EF Core (write)](#3-ef-core-write) | [PNG](img/03_ef_core_write.png) |
| 4 | [Dapper (read)](#4-dapper-read) | [PNG](img/04_dapper_read.png) |
| 5 | [UnitOfWork](#5-unitofwork) | [PNG](img/05_unit_of_work.png) |
| 6 | [Idempotency storage](#6-idempotency-storage) | [PNG](img/06_idempotency_storage.png) |
| 7 | [Eventos](#7-eventos) | [PNG](img/07_eventos.png) |
| 8 | [Logs](#8-logs) | [PNG](img/08_logs.png) |
| 9 | [Trace flow](#9-trace-flow) | [PNG](img/09_trace_flow.png) |
| 10 | [Frontend](#10-frontend) | [PNG](img/10_frontend.png) |
| 11 | [Docker](#11-docker) | [PNG](img/11_docker.png) |

---

## 0. Vista general

```mermaid
flowchart LR
    U(["Usuario"]) --> FE

    subgraph D["Docker Compose"]
        FE["<b>Frontend</b> · Next.js · :3000<br/>Torneos · Equipos · Partidos<br/>Tabla de posiciones · Goleadores"]
        API["<b>API</b> · .NET 10 · :5000<br/>/api/torneos · /api/equipos<br/>/api/jugadores · /api/partidos"]
        DB[("<b>SQL Server</b> · base Mundialito<br/>Torneos · Equipos · Jugadores<br/>Partidos · Goles")]
        J["<b>Jaeger</b> · :16686<br/>trazas"]
    end

    FE -->|"HTTP/JSON"| API
    API -->|"Commands · EF Core"| DB
    API -->|"Queries · Dapper"| DB
    API -->|"OpenTelemetry"| J
```

El usuario gestiona el torneo desde el frontend; el frontend llama a la API REST; la API guarda torneos, equipos, jugadores, partidos y goles en SQL Server (escribe con EF Core y lee con Dapper) y envía sus trazas a Jaeger. Todo levanta con `docker-compose up`.

---

## A. Modelo de datos del Mundialito

```mermaid
erDiagram
    Torneos ||--o{ TorneoEquipos : "inscribe"
    Equipos ||--o{ TorneoEquipos : "participa"
    Equipos ||--o{ Jugadores : "tiene"
    PosicionesJuego ||--o{ Jugadores : "juega de"
    Torneos ||--o{ Partidos : "programa"
    Equipos ||--o{ Partidos : "local o visitante"
    Partidos ||--o{ Goles : "registra"
    Jugadores ||--o{ Goles : "anota"

    Torneos {
        guid Id PK
        string Nombre
        date FechaInicio
        date FechaFin
        string Estado "Planificado, EnCurso, Finalizado"
    }
    TorneoEquipos {
        guid TorneoId PK
        guid EquipoId PK
        datetime FechaInscripcion
    }
    Equipos {
        guid Id PK
        string Nombre
        string Abreviatura
        string Departamento
    }
    Jugadores {
        guid Id PK
        guid EquipoId FK
        int PosicionJuegoId FK
        string Nombres
        string Apellidos
        int NumeroCamiseta
    }
    PosicionesJuego {
        int Id PK
        string Nombre "Portero, Defensa, Mediocampista, Delantero"
    }
    Partidos {
        guid Id PK
        guid TorneoId FK
        guid EquipoLocalId FK
        guid EquipoVisitanteId FK
        datetime FechaProgramada
        string Sede
        string Estado "Programado, Jugado, Cancelado"
        int GolesLocal
        int GolesVisitante
    }
    Goles {
        guid Id PK
        guid PartidoId FK
        guid JugadorId FK
        guid EquipoId FK
        int Minuto
    }
```

Un torneo inscribe equipos (`TorneoEquipos`), cada equipo tiene sus integrantes con una posición de juego, el torneo programa partidos entre dos equipos inscritos y cada partido jugado registra sus goles con el jugador y el minuto. La tabla de posiciones y los goleadores no se guardan: se calculan desde `Partidos` y `Goles`. Además existen `EventosDominio` y `RegistrosIdempotencia` (diagramas 6 y 7).

---

## B. Ciclo de un torneo

```mermaid
flowchart LR
    subgraph P["Preparación"]
        direction TB
        T1["<b>1. Crear el torneo</b><br/>POST /api/torneos<br/>estado Planificado"]
        T2["<b>2. Inscribir equipos</b><br/>POST /api/torneos/{id}/equipos"]
        T3["<b>3. Registrar integrantes</b><br/>POST /api/equipos/{id}/jugadores"]
        T4["<b>4. Programar partidos</b><br/>POST /api/partidos<br/>estado Programado"]
        T1 --> T2 --> T3 --> T4
    end
    subgraph C["Competencia"]
        direction TB
        T5["<b>5. Iniciar el torneo</b><br/>PATCH /api/torneos/{id}<br/>→ EnCurso"]
        T6["<b>6. Registrar resultados</b><br/>POST /api/partidos/{id}/resultado<br/>→ Jugado"]
        T7["<b>7. Tabla y goleadores</b><br/>GET …/tabla-posiciones<br/>GET …/goleadores"]
        T8["<b>8. Cerrar el torneo</b><br/>PATCH /api/torneos/{id}<br/>→ Finalizado"]
        T5 --> T6 --> T7 --> T8
    end
    P --> C
```

Este es el recorrido funcional completo de un torneo. Los estados solo avanzan (un torneo `Finalizado` no vuelve a `EnCurso`) y un torneo finalizado ya no acepta inscripciones, partidos ni resultados. Los datos iniciales dejan el sistema en el paso 6: *Mundialito Corporativo 2026* en curso, 4 equipos, 20 jugadores y 6 partidos, 3 ya jugados.

---

## 1. Capas

```mermaid
flowchart TB
    API["<b>Api</b><br/>TorneosController<br/>EquiposController<br/>JugadoresController<br/>PartidosController<br/>PosicionesJuegoController<br/>Middlewares de trazas e idempotencia"]
    APP["<b>Application</b><br/>Módulos: Torneos · Equipos<br/>Jugadores · Partidos<br/>19 Commands · 12 Queries<br/>IUnitOfWork · I…Repository<br/>I…Queries"]
    DOM["<b>Domain</b><br/>Torneo · Equipo · Jugador<br/>Partido · Gol<br/>Reglas RN-01 a RN-23<br/>TablaPosicionesCalculator"]
    INF["<b>Infrastructure</b><br/>MundialitoDbContext · EF Core<br/>Repositorios · UnitOfWork<br/>TorneoQueries · Dapper<br/>Scripts 001 y 002 · Seed"]

    API --> APP
    APP --> DOM
    INF -.->|"implementa"| APP
```

Las reglas del Mundialito (por ejemplo, que un goleador debe pertenecer a su equipo o que un equipo no juega dos partidos a la misma hora) viven en Domain, sin depender de nada. Application organiza los casos de uso por módulo; Infrastructure resuelve la base de datos; Api solo recibe HTTP y orquesta. Las pruebas de arquitectura verifican estas dependencias.

---

## 2. CQRS

```mermaid
flowchart LR
    C["Controllers"] --> D["MediatR · ISender<br/>pipeline: traza · log · validación"]

    subgraph W["<b>19 Commands</b> · cambian datos · EF Core"]
        W1["Torneos: Create · Update · Patch · Delete<br/>RegisterEquipoInTorneo · WithdrawEquipoFromTorneo"]
        W2["Equipos: Create · Update · Patch · Delete"]
        W3["Jugadores: Register · Update · Patch · Delete"]
        W4["Partidos: Schedule · Update · Patch · Delete<br/>RegisterPartidoResult"]
    end

    subgraph R["<b>12 Queries</b> · solo leen · Dapper"]
        R1["Torneos: GetTorneos · GetTorneoById · GetTorneoEquipos<br/>GetTablaPosiciones · GetGoleadores"]
        R2["Equipos: GetEquipos · GetEquipoById"]
        R3["Jugadores: GetJugadores · GetJugadorById"]
        R4["Partidos: GetPartidos · GetPartidoById<br/>PosicionesJuego: GetPosicionesJuego"]
    end

    D -->|"Send(command)"| W
    D -->|"Send(query)"| R
```

Todo lo que cambia el torneo (inscribir un equipo, programar un partido, registrar un resultado) es un Command; todo lo que se consulta (tabla, goleadores, calendario) es una Query. MediatR envía cada uno a su handler a través del pipeline: `ObservabilityBehavior` abre el span y registra la duración, y `ValidationBehavior` valida la entrada antes del handler.

---

## 3. EF Core (write)

```mermaid
flowchart LR
    R["POST /api/partidos/{id}/resultado<br/>Finanzas United 2 - 1 RR. HH. Athletic<br/>goleadores y minutos"] --> H["RegisterPartidoResult<br/>CommandHandler"]
    H -->|"EF Core: carga"| L["Partido · Torneo<br/>jugadores goleadores"]
    L --> P{"partido.RegisterResult()<br/>¿cumple las reglas?"}
    P -->|"No"| E["400 / 409<br/>no se guarda nada"]
    P -->|"Sí"| U["UnitOfWork<br/>CommitAsync()"]
    U --> DB[("UPDATE Partidos → Jugado 2-1<br/>INSERT Goles × 3<br/>INSERT EventosDominio")]
```

Para registrar el resultado de un partido pendiente se cargan el partido, su torneo y los goleadores con EF Core. El dominio valida las reglas: el partido debe estar `Programado`, el torneo no puede estar finalizado, los goles deben coincidir con el marcador, cada goleador debe ser de su equipo y el minuto debe estar entre 1 y 130. Si algo falla responde 400 o 409 sin guardar; si todo es correcto, el Unit of Work guarda el partido, los goles y el evento.

---

## 4. Dapper (read)

```mermaid
flowchart LR
    subgraph T["GET /api/torneos/{id}/tabla-posiciones"]
        T1["Partidos jugados<br/>del torneo"] --> T2["2 filas por partido<br/>local y visitante"] --> T3["Suma por equipo<br/>PJ G E P GF GC DG Pts"] --> T4["Orden: Pts · DG · GF · nombre<br/>OFFSET / FETCH"]
    end
    subgraph G["GET /api/torneos/{id}/goleadores"]
        G1["Goles del torneo"] --> G2["JOIN Jugadores<br/>y Equipos"] --> G3["Goles por jugador<br/>orden descendente"]
    end
    T4 --> O["PagedResponse<br/>data · pageNumber · pageSize<br/>totalRecords · totalPages"]
    G3 --> O
```

La tabla de posiciones y los goleadores se calculan en SQL con Dapper cada vez que se consultan. Con los datos iniciales la tabla queda: Operaciones City 4 pts (DG +3), Tecnología FC 3 (−2), Recursos Humanos Athletic 1 (0) y Finanzas United 0 (−1). La paginación y el orden se hacen en la base y `sortBy` solo acepta columnas de una lista blanca.

---

## 5. UnitOfWork

```mermaid
flowchart TB
    H["RegisterPartidoResult<br/>CommandHandler"] -->|"CommitAsync()"| T["BEGIN TRAN"]
    T --> S["UPDATE Partidos · INSERT Goles<br/>INSERT EventosDominio"]
    S --> E{"¿Error?"}
    E -->|"No"| C["COMMIT<br/>y despacha PartidoResultRegistered"]
    E -->|"Otro usuario modificó<br/>el partido (VersionFila)"| R1["ROLLBACK · 409<br/>Database.ConcurrencyConflict"]
    E -->|"Viola un índice único o una FK<br/>(ej. dos altas simultáneas<br/>con la misma camiseta)"| R2["ROLLBACK · 409<br/>Database.ConstraintViolation"]
```

El resultado, los goles y el evento se guardan juntos: o queda todo o no queda nada, así nunca hay un partido `Jugado` sin sus goles. Si dos personas registran a la vez sobre el mismo partido, la segunda recibe 409 en lugar de sobrescribir.

---

## 6. Idempotency storage

```mermaid
flowchart TB
    P["POST /api/partidos/{id}/resultado<br/>Idempotency-Key: 6f1c2a54-…"] --> K{"¿Trae la clave?"}
    K -->|"No"| B["400"]
    K -->|"Sí"| E{"¿Existe en<br/>RegistrosIdempotencia?"}
    E -->|"No"| N["Reserva EnProceso<br/>y registra el resultado"]
    N --> G["Guarda el 201 y su cuerpo<br/>estado Completado"]
    E -->|"Sí, mismo cuerpo<br/>(doble clic o reintento)"| O["Devuelve el mismo 201<br/>Idempotent-Replayed: true<br/>sin duplicar goles"]
    E -->|"Sí, otro cuerpo"| X["409 Conflict"]
```

Si el usuario hace doble clic en «Registrar resultado» o la red reintenta la petición, la segunda llamada recibe la misma respuesta y los goles no se registran dos veces. Lo mismo protege la creación de torneos, equipos, jugadores, partidos e inscripciones: todo POST lleva su clave.

---

## 7. Eventos

```mermaid
flowchart TB
    EV["<b>Eventos del dominio</b><br/><br/><b>EquipoCreated</b><br/>crear un equipo<br/><b>JugadorRegistered</b><br/>registrar un integrante<br/><b>PartidoScheduled</b><br/>programar un partido<br/><b>PartidoResultRegistered</b><br/>registrar un resultado"]
    EV --> U["UnitOfWork<br/>misma transacción"]
    U --> T[("EventosDominio<br/>con TraceId y CorrelationId")]
    U -->|"después del commit"| H1["EquipoCreatedDomainEventHandler<br/>log: Equipo creado"]
    U -->|"después del commit"| H2["PartidoResultRegisteredDomainEventHandler<br/>log: Resultado registrado"]
```

El dominio emite cuatro eventos y todos quedan guardados en `EventosDominio`, con el request que los originó. Los dos que pide el requerimiento, equipo creado y resultado registrado, tienen además un handler que escribe un log; los otros dos quedan disponibles para futuras reacciones, como notificar a los equipos.

---

## 8. Logs

```mermaid
flowchart TB
    R["POST /api/partidos/{id}/resultado"] --> L1["Command RegisterPartidoResultCommand<br/>ejecutado en 18 ms"]
    L1 --> L2["Evento de dominio PartidoResultRegisteredDomainEvent<br/>registrado para Partido {id}"]
    L2 --> L3["Resultado registrado en el partido {id} del torneo {id}:<br/>{EquipoLocalId} 2 - 1 {EquipoVisitanteId}"]
    L3 --> L4["HTTP POST /api/partidos/{id}/resultado<br/>respondió 201 en 35 ms"]
    L4 --> S["<b>Serilog · JSON</b><br/>los cuatro logs con el mismo<br/>TraceId y CorrelationId"]
```

Registrar un resultado deja cuatro logs estructurados, uno por cada etapa. Como todos comparten el TraceId, se puede filtrar y ver la historia completa de ese registro. Los 4xx y las peticiones de más de 500 ms se registran como Warning y los 5xx como Error.

---

## 9. Trace flow

```mermaid
flowchart LR
    FE["Frontend<br/>X-Correlation-Id"] --> S1["<b>POST api/partidos/{id}/resultado</b><br/>span HTTP · TraceId"]
    S1 --> S2["Command<br/>RegisterPartidoResultCommand"]
    S2 --> Q1["SELECT Partidos<br/>Torneos · Jugadores"]
    S2 --> Q2["UPDATE Partidos · INSERT Goles<br/>INSERT EventosDominio"]
    S1 --> S3["Query GetPartidoByIdQuery"]
    S3 --> Q3["SELECT Partidos + Goles"]
    S1 -.->|"OTLP"| J["Jaeger<br/>servicio mundialito-api"]
```

Así se ve en Jaeger el registro de un resultado: el request HTTP contiene el span del Command, que contiene las consultas SQL de lectura y escritura, y al final la Query que arma la respuesta 201. El mismo TraceId aparece en los logs, en `EventosDominio` y en cualquier error (ProblemDetails).

---

## 10. Frontend

```mermaid
flowchart LR
    subgraph FE["Páginas · Next.js"]
        P0["Inicio<br/>resumen del torneo"]
        P1["/torneos<br/>alta · inicio y cierre · inscripción"]
        P2["/equipos<br/>equipos e integrantes"]
        P3["/partidos<br/>calendario · programar · resultado"]
        P4["/tabla-posiciones"]
        P5["/goleadores"]
    end
    subgraph API["API REST"]
        A1["/api/torneos"]
        A2["/api/equipos · /api/jugadores"]
        A3["/api/partidos"]
        A4["/api/torneos/{id}/tabla-posiciones"]
        A5["/api/torneos/{id}/goleadores"]
    end
    P0 --> A1
    P1 --> A1
    P2 --> A2
    P3 --> A3
    P4 --> A4
    P5 --> A5
```

Cada sección del frontend corresponde a un recurso de la API. Las páginas leen con Server Components y los formularios (crear, editar, eliminar, registrar resultado) escriben con Server Actions; en ambos casos es el servidor de Next.js el que llama a la API, nunca el navegador.

---

## 11. Docker

```mermaid
flowchart LR
    subgraph DC["docker-compose up"]
        DB[("<b>db</b> · SQL Server 2022<br/>:14330 · volumen mundialito-data")]
        API["<b>api</b> · .NET 10 · :5000<br/>1. espera a que db esté healthy<br/>2. crea la base con los scripts 001 y 002<br/>3. seed: 1 torneo, 4 equipos,<br/>20 jugadores, 6 partidos"]
        FE["<b>frontend</b> · Next.js · :3000"]
        J["<b>jaeger</b> · v2 · :16686"]
    end
    DB -->|"healthcheck OK"| API
    FE -->|"http://api:8080"| API
    API -->|"OTLP :4317"| J
```

Con un solo comando quedan los cuatro contenedores y el Mundialito listo para usar: la API espera a la base, crea el esquema, carga el torneo de ejemplo y el frontend ya muestra la tabla de posiciones.

---

## Cómo regenerar las imágenes

El código Mermaid de este archivo es la fuente. Para volver a exportar un diagrama, copiar su bloque a un archivo `.mmd` y ejecutar:

```bash
npx @mermaid-js/mermaid-cli -i diagrama.mmd -o img/diagrama.png -b white -s 2
```
