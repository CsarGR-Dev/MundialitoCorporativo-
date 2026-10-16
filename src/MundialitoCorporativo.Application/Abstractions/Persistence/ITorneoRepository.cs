using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Abstractions.Persistence;

/// <summary>
/// Acceso de escritura al agregado Torneo (incluye sus inscripciones). Los cambios se confirman con <see cref="IUnitOfWork"/>.
/// </summary>
public interface ITorneoRepository
{
    Task<Torneo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Torneo?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNombreAsync(string nombre, Guid? excludeId = null, CancellationToken cancellationToken = default);

    void Add(Torneo torneo);
}
