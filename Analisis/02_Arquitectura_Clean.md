# Ítem 2 – Arquitectura Obligatoria (Clean Architecture)

## Requerimiento

Capas mínimas: Domain, Application, Infrastructure, Api.

Reglas:
- Domain no depende de ninguna otra capa.
- Application depende solo de Domain.
- Infrastructure implementa contratos.
- Api solo orquesta.

## Referencias entre proyectos

```
Api ──────────────► Application ──► Domain
 │                       ▲
 └──► Infrastructure ────┘ (implementa interfaces de Application)
```

| Proyecto | Referencia a | Paquetes NuGet permitidos |
|---|---|---|
| Domain | Ninguno | Ninguno (C# puro) |
| Application | Domain | FluentValidation, Microsoft.Extensions.Logging.Abstractions, Microsoft.Extensions.DependencyInjection.Abstractions |
| Infrastructure | Application, Domain | EF Core, EF Core SqlServer, Dapper, Microsoft.Data.SqlClient |
| Api | Application, Infrastructure | Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore, Serilog.AspNetCore, OpenTelemetry |

La referencia de Api a Infrastructure existe únicamente para el registro de dependencias (composition root) en `Program.cs`. Los controladores nunca usan clases de Infrastructure directamente.

## Qué debe llevar cada capa

### Domain (implementado)
```
Domain/
├── Common/
│   ├── Entity.cs, AggregateRoot.cs, SoftDeletableAggregateRoot.cs
│   ├── IDomainEvent.cs (+ DomainEvent)
│   ├── Result.cs, ResultOfT.cs
│   ├── Error.cs, ErrorType.cs, ValidationError.cs
│   └── SequentialGuid.cs, Guard.cs
├── Torneos/
│   ├── Torneo.cs              (Create, Update, ChangeStatus, RegisterEquipo, WithdrawEquipo, EnsureAcceptsPartidoOn, Delete)
│   ├── TorneoEquipo.cs        (inscripción)
│   ├── EstadoTorneo.cs
│   └── TorneoErrors.cs
├── Equipos/
│   ├── Equipo.cs              (Create, Update, Delete)
│   ├── EquipoErrors.cs
│   └── Events/EquipoCreatedDomainEvent.cs
├── PosicionesJuego/
│   ├── PosicionJuego.cs       (catálogo)
│   └── PosicionJuegoErrors.cs
├── Jugadores/
│   ├── Jugador.cs             (Create, Update, Delete)
│   ├── JugadorErrors.cs
│   └── Events/JugadorRegisteredDomainEvent.cs
├── Partidos/
│   ├── Partido.cs             (Schedule, Update, Cancel, RegisterResult, Delete)
│   ├── Gol.cs (+ DetalleGol)
│   ├── EstadoPartido.cs
│   ├── PartidoErrors.cs
│   └── Events/PartidoScheduledDomainEvent.cs, PartidoResultRegisteredDomainEvent.cs
├── TablaPosiciones/
│   ├── TablaPosicionesCalculator.cs  (lógica pura de puntos y orden, testeable)
│   └── ScoringRules.cs
└── AssemblyReference.cs
```
- Modelos en español (mismos nombres que tablas y columnas); clases base, métodos y servicios en inglés.
- Entidades con constructores privados y métodos de fábrica que devuelven `Result<T>`.
- Setters privados; las invariantes se validan dentro de la entidad.
- Sin atributos de EF ni referencias a frameworks.

### Application (planificado)
```
Application/
├── Abstractions/
│   ├── Messaging/ (ICommand, ICommand<T>, IQuery<T>, ICommandHandler, IQueryHandler, IDispatcher)
│   ├── Persistence/ (IUnitOfWork, ITorneoRepository, IEquipoRepository, IJugadorRepository, IPartidoRepository)
│   ├── Data/ (IDbConnectionFactory)
│   ├── Idempotency/ (IIdempotencyStore)
│   └── Time/ (IDateTimeProvider)
├── Common/
│   ├── Paging/ (PagedQuery, PagedResponse<T>, SortDirection)
│   └── Behaviors/ (validación y logging alrededor de handlers)
├── Torneos/
│   ├── Commands/ CreateTorneo, UpdateTorneo, PatchTorneo, DeleteTorneo, RegisterEquipo, WithdrawEquipo
│   └── Queries/ GetTorneos, GetTorneoById, GetTorneoEquipos
├── PosicionesJuego/Queries/ GetPosicionesJuego
├── Equipos/
│   ├── Commands/ CreateEquipo, UpdateEquipo, PatchEquipo, DeleteEquipo
│   └── Queries/ GetEquipos, GetEquipoById
├── Jugadores/ ...
├── Partidos/
│   ├── Commands/ SchedulePartido, RegisterPartidoResult, ...
│   └── Queries/ GetPartidos, GetPartidoById
├── TablaPosiciones/Queries/ GetTablaPosiciones
├── Goleadores/Queries/ GetGoleadores
├── AssemblyReference.cs
└── DependencyInjection.cs   (AddApplication)
```
- Un handler por caso de uso (`CreateEquipoCommand` → `CreateEquipoCommandHandler`).
- Validadores FluentValidation por comando.
- DTOs de salida propios de Application (`EquipoResponse`); nunca se exponen entidades.

### Infrastructure (planificado)
```
Infrastructure/
├── Persistence/
│   ├── MundialitoDbContext.cs
│   ├── Configurations/ (IEntityTypeConfiguration por entidad)
│   ├── Repositories/ (EquipoRepository, PartidoRepository, ...)
│   └── UnitOfWork.cs
├── Queries/ (consultas Dapper)
├── Data/SqlConnectionFactory.cs
├── Idempotency/SqlIdempotencyStore.cs
├── DomainEvents/ (despacho y registro de eventos)
├── Seed/DatabaseSeeder.cs
├── AssemblyReference.cs
└── DependencyInjection.cs   (AddInfrastructure)
```

### Api (planificado)
```
Api/
├── Controllers/ (TorneosController, EquiposController, JugadoresController, PartidosController, PosicionesJuegoController)
├── Middleware/ (CorrelationIdMiddleware, RequestTimingMiddleware, manejo global de excepciones)
├── Filters/ (IdempotencyFilter)
├── Extensions/ResultExtensions.cs (Result → IActionResult)
├── Contracts/ (cuerpos de request de la API)
├── AssemblyReference.cs
└── Program.cs
```
- Los controladores solo: reciben el request, lo mapean a Command/Query, invocan el despachador y traducen el `Result` a HTTP.
- Sin lógica de negocio ni acceso a datos en controladores.
- Convención de nombres completa: `17_Convencion_Nombres.md`.

## Verificación automática (valor agregado)

Agregar pruebas de arquitectura con **NetArchTest** en el proyecto de pruebas:
- Domain no referencia a Application, Infrastructure ni Api.
- Application no referencia a Infrastructure ni Api.
- Los handlers de Query no dependen de `DbContext`.
- Los controladores no dependen de Infrastructure.

## Criterios de aceptación

- [ ] La solución compila con cuatro proyectos y referencias en una sola dirección.
- [ ] Domain no tiene paquetes NuGet.
- [ ] Ningún controlador contiene `if` de reglas de negocio.
- [ ] Interfaces definidas en Application e implementadas en Infrastructure.
- [ ] Pruebas de arquitectura en verde.
