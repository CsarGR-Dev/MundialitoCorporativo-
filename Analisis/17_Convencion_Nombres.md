# Convención de Nombres

## 1. Regla general

| Va en **español** | Va en **inglés** |
|---|---|
| **Los modelos**: entidades, sus propiedades y enumerados, con el mismo nombre que las tablas, columnas y valores de la base de datos. | **Todo lo técnico**, por convención de .NET y de los patrones: clases base, Result Pattern, métodos, interfaces, servicios, repositorios, Commands/Queries, handlers, controladores, pruebas. |

Así el modelo refleja exactamente la base de datos (`Equipo.Nombre` ↔ `Equipos.Nombre`, `EstadoPartido.Jugado` ↔ `'Jugado'`) y el código sigue la convención que cualquier desarrollador .NET reconoce.

## 2. En español: el modelo

| Elemento | Ejemplos | Tabla / columna |
|---|---|---|
| Entidades | `Torneo`, `TorneoEquipo`, `Equipo`, `Jugador`, `Partido`, `Gol`, `PosicionJuego` | `Torneos`, `TorneoEquipos`, `Equipos`, `Jugadores`, `Partidos`, `Goles`, `PosicionesJuego` |
| Propiedades | `Nombre`, `Abreviatura`, `Departamento`, `EquipoLocalId`, `FechaProgramada`, `GolesLocal`, `Eliminado`, `FechaEliminacion` | Columnas con el mismo nombre |
| Enumerados y valores | `EstadoPartido.Programado / Jugado / Cancelado`, `EstadoTorneo.Planificado / EnCurso / Finalizado` | Columna `Estado` |
| Filas de lectura (DTO de consultas) | `FilaTablaPosiciones` (`Posicion`, `PartidosJugados`, `DiferenciaGol`, `Puntos`) | Vista `vw_TablaPosiciones` |
| Datos de entrada del modelo | `DetalleGol(JugadorId, EquipoId, Minuto)` | Columnas de `Goles` |
| Carpetas / espacios de nombres de cada modelo | `Torneos`, `Equipos`, `Jugadores`, `Partidos`, `PosicionesJuego`, `TablaPosiciones` | — |
| Rutas de la API (recursos) | `/api/torneos`, `/api/equipos`, `/api/partidos`, `/api/torneos/{id}/tabla-posiciones` | — |
| Filtros por entidad (propiedades) | `nombre`, `departamento`, `equipoId`, `estado`, `fechaDesde`, `fechaHasta` | — |
| Mensajes para el usuario | "Ya existe un equipo con el nombre…" | — |

## 3. En inglés: lo técnico

| Elemento | Convención | Ejemplos |
|---|---|---|
| Capas (requerimiento ítem 2) | Nombre del requerimiento | `Domain`, `Application`, `Infrastructure`, `Api` |
| Clases base | Nombre del patrón | `Entity`, `AggregateRoot`, `SoftDeletableAggregateRoot`, `IDomainEvent`, `DomainEvent` |
| Result Pattern (ítem 5) | Nombre del patrón | `Result`, `Result<TValue>`, `Success`, `Failure`, `IsSuccess`, `IsFailure`, `Value`, `ErrorMessage`, `ErrorCode`, `Error`, `ErrorType`, `ValidationError` |
| Métodos | Verbo en inglés + modelo cuando aplique | `Create`, `Update`, `Delete`, `Schedule`, `Cancel`, `RegisterResult`, `RegisterEquipo`, `WithdrawEquipo`, `ChangeStatus` |
| Constantes de reglas | Propiedad + sufijo en inglés | `NombreMaxLength`, `NumeroCamisetaMin`, `GolesMax` |
| Catálogos de errores | Modelo + `Errors` | `EquipoErrors`, `PartidoErrors`, `TorneoErrors` |
| Miembros y códigos de error | Regla en inglés + propiedad del modelo | `EquipoErrors.DuplicateNombre` → código `"Equipo.DuplicateNombre"` |
| Eventos de dominio | Modelo + hecho en inglés + `DomainEvent` | `EquipoCreatedDomainEvent`, `PartidoResultRegisteredDomainEvent` |
| Servicios de dominio | Nombre descriptivo | `TablaPosicionesCalculator`, `ScoringRules`, `SequentialGuid`, `Guard` |
| Commands / Queries (ítem 3) | Verbo + modelo + sufijo | `CreateEquipoCommand`, `RegisterPartidoResultCommand`, `GetEquiposQuery`, `GetTablaPosicionesQuery` |
| Handlers | Command/Query + `Handler` | `CreateEquipoCommandHandler` |
| Repositorios | `I` + modelo + `Repository` | `IEquipoRepository`, `EquipoRepository` |
| Unit of Work (ítem 4) | Nombre del requerimiento | `IUnitOfWork`, `UnitOfWork` |
| DTO de respuesta | Modelo + `Response` | `EquipoResponse`, `PartidoResponse` |
| Controladores | Recurso + `Controller` | `EquiposController`, `PartidosController` |
| Registro de dependencias | Convención de .NET | `DependencyInjection.AddApplication()`, `AddInfrastructure()` |
| Infraestructura transversal | Nombre técnico | `AssemblyReference`, `CorrelationIdMiddleware`, `IdempotencyFilter` |
| Parámetros técnicos | camelCase en inglés | `utcNow`, `cancellationToken` |
| Parámetros que son datos del modelo | Igual a la propiedad | `nombre`, `fechaProgramada`, `equipoId` |
| Variables locales | camelCase en inglés | `result`, `validation`, `scheduledAt` |
| Campos privados | `_` + camelCase (colecciones del modelo en español) | `_domainEvents`, `_equipos`, `_goles` |
| Pruebas | Clase + `Tests`; método `Method_Scenario_ExpectedResult` | `EquipoTests.Create_WithInvalidNombre_ShouldFail` |
| Parámetros de paginación (ítem 10) | Nombres del requerimiento | `pageNumber`, `pageSize`, `sortBy`, `sortDirection`, `data`, `totalRecords`, `totalPages` |
| Encabezados (ítems 6 y 7) | Nombres del requerimiento | `Idempotency-Key`, `CorrelationId`, `TraceId` |

## 4. Ejemplo

```csharp
public sealed class Equipo : SoftDeletableAggregateRoot              // modelo en español, base en inglés
{
    public const int NombreMaxLength = 100;                            // propiedad + sufijo técnico

    public string Nombre { get; private set; }                         // = columna Equipos.Nombre

    public static Result<Equipo> Create(string nombre, string abreviatura, string? departamento)
    {
        ...
        equipo.Raise(new EquipoCreatedDomainEvent(equipo.Id, equipo.Nombre, equipo.Abreviatura));
        return equipo;
    }
}
```
