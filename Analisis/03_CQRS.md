# Ítem 3 – CQRS (Obligatorio)

## Requerimiento

**Commands (Write Side)**
- Implementados con EF Core.
- Uso obligatorio de Unit of Work.
- Transacciones correctamente manejadas.

**Queries (Read Side)**
- Implementadas exclusivamente con Dapper.
- Consultas optimizadas.
- Sin uso de EF para lectura.
- Paginación eficiente desde base de datos.

## Diseño

### Contratos de mensajería (Application/Abstractions/Messaging)

```csharp
public interface ICommand : IBaseCommand { }
public interface ICommand<TResponse> : IBaseCommand { }
public interface IQuery<TResponse> { }

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken ct);
}
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct);
}
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken ct);
}
```

Se implementa un `Dispatcher` propio que resuelve el handler desde el contenedor de DI y aplica decoradores (validación y logging). Esto deja explícita la separación sin depender de librerías con licencia comercial.

### Lado de escritura (Commands)

| Command | Handler usa | Resultado |
|---|---|---|
| `CreateEquipoCommand` | `IEquipoRepository` + `IUnitOfWork` | `Result<Guid>` |
| `UpdateTeamCommand` (PUT) | idem | `Result` |
| `PatchTeamCommand` | idem | `Result` |
| `DeleteTeamCommand` | idem + `IPartidoRepository` | `Result` |
| `RegisterPlayerCommand` | `IJugadorRepository`, `IEquipoRepository` | `Result<Guid>` |
| `UpdatePlayerCommand` / `PatchPlayerCommand` / `DeletePlayerCommand` | idem | `Result` |
| `ScheduleMatchCommand` | `IPartidoRepository`, `IEquipoRepository` | `Result<Guid>` |
| `RescheduleMatchCommand` / `PatchMatchCommand` / `DeleteMatchCommand` | idem | `Result` |
| `RegisterPartidoResultCommand` | `IPartidoRepository`, `IJugadorRepository` | `Result` |

Flujo típico de un Command handler:
1. Validar entrada (decorador de validación → `Result.Failure` con código de validación).
2. Cargar agregados mediante repositorios (EF Core, con tracking).
3. Invocar el método de dominio (devuelve `Result`).
4. Si falla, devolver el error sin persistir.
5. `await unitOfWork.CommitAsync(ct)`.
6. Devolver `Result.Success`.

### Lado de lectura (Queries)

| Query | SQL principal | Resultado |
|---|---|---|
| `GetEquiposQuery` | `SELECT ... FROM Equipos WHERE ... ORDER BY {col} OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY` | `PagedResponse<TeamDto>` |
| `GetEquipoByIdQuery` | `SELECT` por Id + conteo de jugadores | `TeamDetailDto` |
| `GetJugadoresQuery` | `Jugadores JOIN Equipos` con filtros | `PagedResponse<PlayerDto>` |
| `GetPartidosQuery` | `Partidos JOIN Equipos (local/visitante)` con filtros de fecha, equipo, estado | `PagedResponse<MatchDto>` |
| `GetPartidoByIdQuery` | `QueryMultipleAsync`: partido + goles | `MatchDetailDto` |
| `GetTablaPosicionesQuery` | CTE que agrega partidos jugados por equipo | `PagedResponse<StandingDto>` |
| `GetGoleadoresQuery` | `Goles JOIN Jugadores JOIN Equipos GROUP BY` | `PagedResponse<ScorerDto>` |

Tablas, columnas y DTO están en español con los mismos nombres, por lo que Dapper llena los DTO sin alias (salvo columnas calculadas).

Reglas del lado de lectura:
- Los Query handlers dependen de `IDbConnectionFactory` o de interfaces de lectura (`IEquipoQueries`), **nunca** de `DbContext`.
- Las consultas seleccionan solo columnas necesarias (sin `SELECT *`).
- Parámetros siempre parametrizados (`@Param`), nunca concatenación de valores.
- `sortBy` se resuelve contra una lista blanca de columnas.
- Total de registros con `COUNT(*) OVER()` en la misma consulta o con `QueryMultipleAsync` (dos sentencias, un solo roundtrip).
- Índices de apoyo definidos en `Db/001_DDL_CrearBaseDatos.sql` (detalle en `Analisis/16_Analisis_Base_Datos.md`, sección 7).

### Ejemplo de consulta de tabla de posiciones (Dapper)

```sql
-- Tabla de un torneo (@TorneoId). Parte de los equipos inscritos para incluir a los que no han jugado.
-- Los goles se convierten a INT: son TINYINT (0–255) y la diferencia de gol puede ser negativa.
WITH Resultados AS (
    SELECT EquipoLocalId AS EquipoId, CAST(GolesLocal AS INT) AS GF, CAST(GolesVisitante AS INT) AS GC
    FROM Partidos WHERE TorneoId = @TorneoId AND Estado = 'Jugado' AND Eliminado = 0
    UNION ALL
    SELECT EquipoVisitanteId, CAST(GolesVisitante AS INT), CAST(GolesLocal AS INT)
    FROM Partidos WHERE TorneoId = @TorneoId AND Estado = 'Jugado' AND Eliminado = 0
),
Tabla AS (
    SELECT e.Id, e.Nombre,
           COUNT(r.EquipoId)                                                    AS PartidosJugados,
           ISNULL(SUM(CASE WHEN r.GF > r.GC THEN 1 ELSE 0 END), 0)              AS Ganados,
           ISNULL(SUM(CASE WHEN r.GF = r.GC THEN 1 ELSE 0 END), 0)              AS Empatados,
           ISNULL(SUM(CASE WHEN r.GF < r.GC THEN 1 ELSE 0 END), 0)              AS Perdidos,
           ISNULL(SUM(r.GF), 0)                                                 AS GolesFavor,
           ISNULL(SUM(r.GC), 0)                                                 AS GolesContra,
           ISNULL(SUM(r.GF - r.GC), 0)                                          AS DiferenciaGol,
           ISNULL(SUM(CASE WHEN r.GF > r.GC THEN 3 WHEN r.GF = r.GC THEN 1 ELSE 0 END), 0) AS Puntos
    FROM TorneoEquipos te
    JOIN Equipos e ON e.Id = te.EquipoId AND e.Eliminado = 0
    LEFT JOIN Resultados r ON r.EquipoId = te.EquipoId
    WHERE te.TorneoId = @TorneoId
    GROUP BY e.Id, e.Nombre
)
SELECT ROW_NUMBER() OVER (ORDER BY Puntos DESC, DiferenciaGol DESC, GolesFavor DESC, Nombre ASC) AS Posicion,
       Id AS EquipoId, Nombre AS Equipo,
       PartidosJugados, Ganados, Empatados, Perdidos, GolesFavor, GolesContra, DiferenciaGol, Puntos,
       COUNT(*) OVER () AS TotalRecords
FROM Tabla
ORDER BY Posicion
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
```

## Transacciones

- Cada Command se ejecuta dentro de una transacción gestionada por el Unit of Work.
- Los casos con varias entidades (registrar resultado + goles + evento de dominio) se confirman en un único `CommitAsync`, que es atómico.
- Las Queries no abren transacciones explícitas.

## Criterios de aceptación

- [ ] Carpetas `Commands/` y `Queries/` separadas por módulo.
- [ ] Ningún Query handler inyecta `DbContext` (verificado con prueba de arquitectura).
- [ ] Ningún Command handler usa Dapper.
- [ ] Toda paginación se resuelve en SQL (`OFFSET/FETCH`).
- [ ] Todas las consultas Dapper son parametrizadas.
- [ ] Tabla de posiciones y goleadores calculados con SQL, no en memoria.

## Implementación real

- **Despachador propio** (`Application/Messaging/Dispatcher.cs`): resuelve el handler desde el contenedor y ejecuta antes los validadores de FluentValidation; si fallan devuelve `ValidationError` (400) sin invocar el handler.
- **Commands** (`Application/*/Commands/`): un archivo por caso de uso (Command + validador + handler). Usan repositorios EF Core y terminan con `IUnitOfWork.CommitAsync()`.
- **Queries** (`Application/*/Queries/`): los handlers dependen de interfaces de lectura (`Application/Abstractions/Queries/`) que Infrastructure implementa con Dapper (`Infrastructure/Queries/`). Así Application no depende de Dapper y la prueba de arquitectura lo sigue verificando.
- **SQL**: `SqlFilter` arma el WHERE solo con los filtros enviados (parametrizados, `LIKE` con comodines escapados); `SqlSort` traduce `sortBy` a columnas de una lista blanca con desempate estable; `PagedSql` ejecuta conteo y página (`OFFSET/FETCH`) en un solo viaje con `QueryMultipleAsync`.
- **Fechas**: las consultas devuelven `TODATETIMEOFFSET(columna, 0)` para exponerlas en UTC sin ambigüedad.
- **SQL de referencia** para SSMS: `Db/010_QRY_TablaPosiciones.sql` y `Db/011_QRY_Goleadores.sql`.
- **Pruebas**: `UnitTests/Application/` (despachador, handlers, registro de handlers y sortBy) e `IntegrationTests/Cqrs/CqrsSqlServerTests.cs` (pila completa contra SQL Server).
