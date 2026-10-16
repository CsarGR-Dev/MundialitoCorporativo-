using MundialitoCorporativo.Domain.Equipos;

namespace MundialitoCorporativo.Application.Abstractions.Persistence;

/// <summary>
/// Acceso de escritura al agregado Equipo. Los cambios se confirman con <see cref="IUnitOfWork"/>.
/// </summary>
public interface IEquipoRepository
{
    Task<Equipo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Equipo?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNombreAsync(string nombre, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<bool> ExistsByAbreviaturaAsync(string abreviatura, Guid? excludeId = null, CancellationToken cancellationToken = default);

    void Add(Equipo equipo);
}
