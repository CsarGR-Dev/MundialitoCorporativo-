using MundialitoCorporativo.Domain.Jugadores;

namespace MundialitoCorporativo.Application.Abstractions.Persistence;

/// <summary>
/// Acceso de escritura al agregado Jugador. Los cambios se confirman con <see cref="IUnitOfWork"/>.
/// </summary>
public interface IJugadorRepository
{
    Task<Jugador?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Jugador?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Jugador>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> ExistsNumeroCamisetaAsync(Guid equipoId, int numeroCamiseta, Guid? excludeId = null, CancellationToken cancellationToken = default);

    void Add(Jugador jugador);
}
