# Análisis General – Mundialito de Fútbol Corporativo

| Campo | Detalle |
|---|---|
| Proyecto | Sistema de Gestión – Mundialito de Fútbol Corporativo |
| Autor | César González |

---

## 1. Contexto

Se requiere un sistema para el control de un torneo de fútbol corporativo. El sistema debe permitir **registrar** equipos, integrantes, calendario de partidos, resultados y goleadores, y **visualizar** equipos, integrantes, partidos, tabla de posiciones y goleadores.

El documento técnico se centra en el **backend y la arquitectura** (Clean Architecture, CQRS, Unit of Work, Result Pattern, REST idempotente, observabilidad). El frontend es obligatorio (Next.js); su diseño visual y la experiencia de usuario no son el foco: debe ser funcional, desacoplado y manejar errores correctamente.

Se permite establecer criterios propios de funcionamiento cuando la descripción no sea suficientemente clara, sin perder de vista el requerimiento general. Esos criterios quedan documentados en la sección 6 de este análisis.

---

## 2. Alcance

### 2.1 Dentro del alcance

- API REST en .NET con arquitectura en capas (Domain, Application, Infrastructure, Api).
- Escritura con EF Core + Unit of Work; lectura exclusiva con Dapper.
- Gestión de equipos, jugadores, partidos, resultados y goles.
- Cálculo automático de tabla de posiciones y tabla de goleadores.
- Idempotencia en POST mediante header `Idempotency-Key`.
- Logging estructurado, CorrelationId/TraceId, duración de requests y eventos de dominio.
- Seed automático de datos al iniciar con base vacía.
- Frontend Next.js consumiendo la API.
- Dockerfiles y `docker-compose.yml` (API + DB + Frontend).
- Diagrama de arquitectura, colección Postman y pruebas unitarias.

### 2.2 Fuera del alcance

- Autenticación y autorización de usuarios (no se solicita).
- Diseño visual elaborado del frontend.
- Fases de grupos y eliminatorias; cada torneo se modela como liga de todos contra todos.
- Event Bus externo (RabbitMQ, Kafka, etc.); los eventos se manejan en proceso.

---

## 3. Stack tecnológico propuesto

| Componente | Tecnología | Justificación |
|---|---|---|
| Backend | .NET 10 / ASP.NET Core Web API | SDK disponible en el equipo (10.0.400); versión LTS actual. |
| Escritura | Entity Framework Core 10 | Exigido para Commands. |
| Lectura | Dapper | Exigido para Queries. |
| Base de datos | SQL Server 2022 (contenedor Linux) | Estándar en entornos bancarios; soporta `OFFSET/FETCH` y `COUNT(*) OVER()`. |
| Logging | Serilog (sobre `ILogger`) | Logs estructurados, enrichers y sinks a consola/archivo/Seq. |
| Tracing | `Activity` de .NET + OpenTelemetry (opcional) → Jaeger | Punto valorable del documento. |
| Validación | FluentValidation | Validaciones de entrada sin excepciones de control de flujo. |
| Mediador CQRS | MediatR 12.5 (`ISender`, `IPipelineBehavior`, `INotification`) | Estándar para CQRS en .NET; la versión 12.5 es la última con licencia Apache 2.0 (desde la 13 es comercial). Solo lo usan Application y los controllers; Domain no lo conoce. |
| Frontend | Next.js (App Router) + TypeScript | Exigido. |
| Pruebas | xUnit + NetArchTest + NSubstitute; Testcontainers para integración | Estándar de la plataforma; sin librerías con licencia comercial. |
| Contenedores | Docker + Docker Compose | Exigido. |

---

## 4. Estructura de la solución

```
MundialitoCorporativo/
├── MundialitoCorp_Back/
│   ├── src/
│   │   ├── MundialitoCorporativo.Domain/
│   │   ├── MundialitoCorporativo.Application/
│   │   ├── MundialitoCorporativo.Infrastructure/
│   │   └── MundialitoCorporativo.Api/
│   ├── tests/
│   │   ├── MundialitoCorporativo.UnitTests/
│   │   └── MundialitoCorporativo.IntegrationTests/
│   ├── Directory.Build.props
│   └── MundialitoCorporativo.slnx
├── MundialitoCorp_Front/
├── Postman/
├── Analisis/
├── docker-compose.yml
└── README.md
```

---

## 5. Modelo de dominio

### 5.1 Entidades

Los modelos (entidades, propiedades y enums) están en español con los mismos nombres que las tablas y columnas; lo técnico (clases base, métodos, servicios, patrones) está en inglés por convención (ver `17_Convencion_Nombres.md`).

| Entidad (C#) | Tabla (SQL) | Propiedades principales |
|---|---|---|
| **PosicionJuego** (catálogo) | `PosicionesJuego` | Id, Nombre, Abreviatura, Orden, Activo |
| **Torneo** | `Torneos` | Id, Nombre, Descripcion, FechaInicio, FechaFin, Estado, Eliminado, FechaEliminacion |
| **Equipo** | `Equipos` | Id, Nombre, Abreviatura, Departamento, Eliminado, FechaEliminacion |
| **TorneoEquipo** | `TorneoEquipos` | TorneoId, EquipoId, FechaInscripcion |
| **Jugador** | `Jugadores` | Id, EquipoId, PosicionJuegoId, Nombres, Apellidos, NumeroCamiseta, Activo, Eliminado |
| **Partido** | `Partidos` | Id, TorneoId, EquipoLocalId, EquipoVisitanteId, FechaProgramada, Sede, Estado, GolesLocal, GolesVisitante, FechaJugado, Eliminado |
| **Gol** | `Goles` | Id, PartidoId, JugadorId, EquipoId, Minuto |
| **RegistroIdempotencia** | `RegistrosIdempotencia` | ClaveIdempotencia, RutaSolicitud, MetodoSolicitud, HashSolicitud, Estado, CodigoEstadoRespuesta, CuerpoRespuesta, EncabezadosRespuesta, FechaExpiracion |
| **RegistroEventoDominio** | `EventosDominio` | Id, TipoEvento, TipoAgregado, AgregadoId, DatosEvento (JSON), FechaOcurrencia, TraceId, CorrelationId |

Las columnas de auditoría (`FechaCreacion`, `FechaModificacion`) y de concurrencia (`VersionFila`) se manejan en Infrastructure, fuera del dominio.

### 5.2 Enumeraciones

| Enum (C#) | Valores (iguales en C# y en SQL) |
|---|---|
| `EstadoPartido` | `Programado`, `Jugado`, `Cancelado` |
| `EstadoTorneo` | `Planificado`, `EnCurso`, `Finalizado` |

La posición del jugador **no es un enum**: es el catálogo `PosicionesJuego` (Portero, Defensa, Mediocampista, Delantero), ampliable sin cambiar código.

### 5.3 Relaciones

- Un **Torneo** tiene muchos **Equipo** inscritos (a través de **TorneoEquipo**); un **Equipo** puede participar en varios torneos.
- Un **Equipo** tiene muchos **Jugador**; cada **Jugador** tiene una **PosicionJuego**.
- Un **Partido** pertenece a un **Torneo** y referencia dos **Equipo** inscritos en él (local y visitante).
- Un **Partido** jugado tiene muchos **Gol**; cada **Gol** pertenece a un **Jugador** y a un **Equipo**.
- La tabla de posiciones y los goleadores se calculan por torneo.

### 5.4 Eventos de dominio

- `EquipoCreatedDomainEvent` – al crear un equipo (obligatorio).
- `PartidoResultRegisteredDomainEvent` – al registrar el resultado de un partido (obligatorio).
- `JugadorRegisteredDomainEvent` – al registrar un jugador (adicional).
- `PartidoScheduledDomainEvent` – al programar un partido (adicional).

---

## 6. Reglas de negocio y criterios propios

| Código | Regla | Respuesta HTTP si no se cumple |
|---|---|---|
| RN-01 | El nombre del equipo es obligatorio, máximo 100 caracteres y único (sin distinguir mayúsculas). | 400 / 409 |
| RN-02 | El número de camiseta es único dentro de un mismo equipo (1–99). | 400 / 409 |
| RN-03 | Un jugador no puede pertenecer a un equipo inexistente o eliminado. | 404 |
| RN-04 | Un partido requiere dos equipos distintos y existentes. | 400 / 404 |
| RN-05 | Un equipo no puede tener dos partidos programados en la misma fecha y hora. | 409 |
| RN-06 | Solo se registra resultado en partidos con estado `Programado`. Un partido `Jugado` o `Cancelado` no admite nuevo resultado. | 409 |
| RN-07 | Los goles registrados por equipo deben coincidir con el marcador. | 400 |
| RN-08 | El goleador debe pertenecer al equipo al que se le acredita el gol (no se modelan autogoles). | 400 |
| RN-09 | Puntos: victoria 3, empate 1, derrota 0. | — |
| RN-10 | Orden de la tabla: Puntos ↓, Diferencia de gol ↓, Goles a favor ↓; desempate final por nombre ↑ (criterio propio para orden determinista). | — |
| RN-11 | No se puede eliminar un equipo con partidos asociados. | 409 |
| RN-12 | No se puede eliminar un partido ya jugado. | 409 |
| RN-13 | La eliminación es lógica (soft delete): repetir un DELETE sobre un recurso ya eliminado devuelve 204; un id que nunca existió devuelve 404. | 204 / 404 |
| RN-14 | `PUT` no crea recursos (no es upsert); sobre un id inexistente devuelve 404. | 404 |
| RN-15 | Una misma `Idempotency-Key` reutilizada con un cuerpo distinto se rechaza. | 409 |
| RN-16 | El minuto de un gol está entre 1 y 130 (incluye tiempo extra). | 400 |
| RN-17 | Un jugador no cambia de equipo una vez registrado (no se modelan traspasos); `TeamId` no es editable por PUT/PATCH. | 400 |
| RN-18 | Un partido solo puede programarse entre equipos inscritos en el mismo torneo. | 400 |
| RN-19 | En un torneo `Finalizado` no se inscriben equipos, no se programan partidos ni se registran resultados. | 409 |
| RN-20 | Un equipo se inscribe una sola vez por torneo; no se puede retirar si ya tiene partidos en ese torneo. | 409 |
| RN-21 | La fecha de un partido debe estar dentro del rango del torneo (si el torneo tiene fecha fin). | 400 |
| RN-22 | La posición del jugador debe existir en el catálogo y estar activa. | 400 |
| RN-23 | El nombre del torneo es único; la fecha fin no puede ser anterior a la de inicio. | 400 / 409 |

---

## 7. Endpoints previstos

| Verbo | Ruta | Descripción |
|---|---|---|
| GET | `/api/posiciones-juego` | Catálogo de posiciones (paginado) |
| GET | `/api/torneos` | Torneos, paginado, filtros por nombre y estado |
| GET / POST | `/api/torneos`, `/api/torneos/{id}` | Detalle y creación de torneo |
| PUT / PATCH / DELETE | `/api/torneos/{id}` | Reemplazo, cambio de estado o datos, eliminación |
| GET | `/api/torneos/{id}/equipos` | Equipos inscritos, paginado |
| POST | `/api/torneos/{id}/equipos` | Inscribir equipo |
| DELETE | `/api/torneos/{id}/equipos/{equipoId}` | Retirar equipo (sin partidos) |
| GET | `/api/torneos/{id}/tabla-posiciones` | Tabla de posiciones del torneo |
| GET | `/api/torneos/{id}/goleadores` | Goleadores del torneo |
| GET | `/api/equipos` | Lista paginada con filtros y orden dinámico |
| GET | `/api/equipos/{id}` | Detalle de equipo |
| POST | `/api/equipos` | Crear equipo (idempotente con header) |
| PUT | `/api/equipos/{id}` | Reemplazo completo |
| PATCH | `/api/equipos/{id}` | Modificación parcial |
| DELETE | `/api/equipos/{id}` | Eliminación idempotente |
| GET | `/api/equipos/{equipoId}/jugadores` | Jugadores de un equipo, paginado |
| POST | `/api/equipos/{equipoId}/jugadores` | Registrar jugador en un equipo |
| GET | `/api/jugadores` | Búsqueda global de jugadores, paginada |
| GET / PUT / PATCH / DELETE | `/api/jugadores/{id}` | Operaciones sobre un jugador |
| GET | `/api/partidos` | Lista paginada, filtros por torneo, fecha, equipo y estado |
| GET | `/api/partidos/{id}` | Detalle con goles |
| POST | `/api/partidos` | Programar partido (incluye `torneoId`) |
| PUT / PATCH / DELETE | `/api/partidos/{id}` | Reprogramar, cambiar estado, eliminar |
| POST | `/api/partidos/{id}/resultado` | Registrar resultado y goleadores |

---

## 8. Riesgos identificados

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Docker no está en el PATH del equipo de desarrollo. | No se puede validar `docker-compose up`. | Resuelto: Docker Desktop instalado y `docker-compose up` validado (ítem 12). |
| SQL Server tarda en arrancar dentro de Compose. | La API falla al conectarse en el primer intento. | `healthcheck` en el servicio DB + `depends_on: condition: service_healthy` + reintentos en la conexión. |
| Inyección SQL vía `sortBy`. | Seguridad. | Lista blanca de columnas permitidas por consulta. |
| Concurrencia al registrar resultados. | Doble registro. | `RowVersion` (concurrencia optimista) + idempotencia. |

---

## 9. Matriz de trazabilidad (ítem del documento → archivo de análisis)

| # | Ítem del documento técnico | Archivo |
|---|---|---|
| 1 | Objetivo | `01_Objetivo.md` |
| 2 | Arquitectura obligatoria | `02_Arquitectura_Clean.md` |
| 3 | CQRS | `03_CQRS.md` |
| 4 | Unit of Work | `04_Unit_Of_Work.md` |
| 5 | Result Pattern | `05_Result_Pattern.md` |
| 6 | REST completo + idempotencia | `06_REST_Idempotencia.md` |
| 7 | Observabilidad y trazabilidad | `07_Observabilidad_Trazabilidad.md` |
| 8 | Requisitos funcionales | `08_Requisitos_Funcionales.md` |
| 9 | Seed de datos | `09_Seed_Datos.md` |
| 10 | Filtros y paginación | `10_Filtros_Paginacion.md` |
| 11 | Frontend Next.js | `11_Frontend_NextJs.md` |
| 12 | Dockerización | `12_Dockerizacion.md` |
| 13 | Diagrama de arquitectura | `13_Diagrama_Arquitectura.md` |
| 14 | Colección de Postman | `14_Coleccion_Postman.md` |
| 15 | Pruebas unitarias | `15_Pruebas_Unitarias.md` |
| — | Base de datos (complementario) | `16_Analisis_Base_Datos.md` + `Infrastructure/Initialization/Scripts/001_DDL_CrearBaseDatos.sql` |
| — | Convención de nombres (complementario) | `17_Convencion_Nombres.md` |
