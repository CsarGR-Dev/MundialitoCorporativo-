using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Abstractions.Persistence;

/// <summary>
/// Unit of Work (requerimiento ítem 4): único punto donde se confirman los cambios de los repositorios.
/// Centraliza la transacción, registra los eventos de dominio y garantiza que todo se guarde o nada.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Confirma explícitamente, en una sola transacción, todos los cambios pendientes de los repositorios.
    /// Devuelve Failure de tipo Conflict ante conflictos de concurrencia o restricciones de la base de datos.
    /// </summary>
    Task<Result> CommitAsync(CancellationToken cancellationToken = default);
}
