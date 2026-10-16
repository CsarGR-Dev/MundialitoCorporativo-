# Ítem 4 – Unit of Work (Obligatorio)

## Requerimiento

Debe implementarse un patrón Unit of Work que:
- Centralice el manejo de transacciones.
- Permita commit explícito.
- Garantice consistencia en múltiples repositorios.
- Sea usado en todos los Commands.

No se permite guardar cambios directamente desde el DbContext fuera del UnitOfWork.

## Contrato (Application/Abstractions/Persistence)

```csharp
public interface IUnitOfWork
{
    Task<Result> CommitAsync(CancellationToken cancellationToken = default);
}
```

**Decisión de implementación:** la transacción se abre y se cierra dentro de `CommitAsync`. No se exponen `BeginTransaction`/`Rollback` a los handlers: así ningún Command puede dejar una transacción abierta o a medias, y el único punto de commit es explícito (`await unitOfWork.CommitAsync()`).

## Implementación (Infrastructure/Persistence/UnitOfWork.cs)

Responsabilidades de `CommitAsync`:

1. Recolectar los eventos de dominio pendientes de los agregados rastreados por el `ChangeTracker`.
2. Persistir cada evento en la tabla `EventosDominio` (mismo contexto, misma transacción).
3. Asignar campos de auditoría (`FechaCreacion`, `FechaModificacion`, shadow properties) mediante `IDateTimeProvider`.
4. Ejecutar `SaveChangesAsync` dentro de una transacción (`BeginTransactionAsync` si no hay una activa).
5. Confirmar la transacción.
6. Limpiar los eventos de los agregados.
7. Registrar cada evento en el log con propiedades estructuradas (`EventType`, `EventId`, `AggregateType`, `AggregateId`).

Manejo de errores convertidos en `Result` (no se propagan como flujo de control):

| Excepción de infraestructura | Resultado devuelto | HTTP |
|---|---|---|
| `DbUpdateConcurrencyException` (rowversion `VersionFila`) | Rollback + `Error.Conflict(..., "Database.ConcurrencyConflict")` | 409 |
| `DbUpdateException` por índice único, FK o CHECK (SqlException 2601/2627/547) | Rollback + `Error.Conflict(..., "Database.ConstraintViolation")` | 409 |
| Otra excepción | La transacción se revierte al liberarse y la excepción sigue (error técnico real, lo atrapa el middleware global → 500) | 500 |

Las excepciones se capturan solo en este punto de frontera con la base de datos, para traducirlas a `Result`. No se usan para decidir reglas de negocio.

## Restricción de SaveChanges

- `MundialitoDbContext` se registra como `scoped` y solo lo usan los repositorios y el `UnitOfWork`.
- Los repositorios **no** exponen `Save`/`SaveChanges`; solo `Add`, `Remove`, `GetByIdAsync`, `ExistsAsync`, etc.
- `MundialitoDbContext.SaveChanges()` (síncrono) lanza `InvalidOperationException`: obliga a usar el UoW (probado en `UnitOfWorkTests`).
- Repositorios y `UnitOfWork` son `internal`: solo Infrastructure los instancia.

## Uso en los Commands

```csharp
internal sealed class CreateEquipoCommandHandler(
    IEquipoRepository equipoRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateEquipoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateEquipoCommand command, CancellationToken cancellationToken)
    {
        if (await equipoRepository.ExistsByNombreAsync(command.Nombre, cancellationToken: cancellationToken))
        {
            return EquipoErrors.DuplicateNombre(command.Nombre);
        }

        var equipoResult = Equipo.Create(command.Nombre, command.Abreviatura, command.Departamento);
        if (equipoResult.IsFailure)
        {
            return equipoResult.Error;
        }

        equipoRepository.Add(equipoResult.Value);

        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess ? equipoResult.Value.Id : commit.Error;
    }
}
```

## Consistencia en múltiples repositorios

Caso de referencia: **registrar resultado de partido**.
- Se modifica `Match` (marcador y estado).
- Se agregan varios `Goal`.
- Se genera `PartidoResultRegisteredDomainEvent`.
- Todo se confirma en un solo `CommitAsync`. Si algo falla, no queda ni el marcador ni los goles a medias.

## Pruebas requeridas (ver ítem 15)

- Commit exitoso persiste todos los cambios de varios repositorios.
- Si `SaveChanges` falla, se hace rollback y no se persiste nada.
- Conflicto de concurrencia devuelve `Result.Failure` con tipo Conflict.
- Los eventos de dominio se guardan y se despachan solo tras commit exitoso.
- Sin llamada a `CommitAsync`, no hay cambios persistidos.

## Criterios de aceptación

- [ ] Existe `IUnitOfWork` en Application y su implementación en Infrastructure.
- [ ] Todos los Command handlers llaman a `CommitAsync`.
- [ ] No hay llamadas a `SaveChanges` fuera de `UnitOfWork`.
- [ ] Transacción explícita y rollback ante fallo.
- [ ] Eventos de dominio persistidos dentro de la misma transacción.


## Implementación real

- `src/MundialitoCorporativo.Application/Abstractions/Persistence/IUnitOfWork.cs`
- `src/MundialitoCorporativo.Infrastructure/Persistence/UnitOfWork.cs`
- Pruebas: `tests/MundialitoCorporativo.UnitTests/Infrastructure/UnitOfWorkTests.cs` (9) y `tests/MundialitoCorporativo.IntegrationTests/Persistence/UnitOfWorkSqlServerTests.cs` (4, contra SQL Server con el esquema de `Db/001`).
