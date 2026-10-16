using MundialitoCorporativo.Domain.Partidos;

namespace MundialitoCorporativo.Application.Abstractions.Persistence;

/// <summary>
/// Acceso de escritura al agregado Partido (incluye sus goles). Los cambios se confirman con <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPartidoRepository
{
    Task<Partido?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Partido?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// RN-05: indica si alguno de los equipos ya tiene un partido activo (programado o jugado) a esa hora,
    /// como local o como visitante.
    /// </summary>
    Task<bool> ExistsAtSameTimeAsync(
        Guid equipoLocalId,
        Guid equipoVisitanteId,
        DateTime fechaProgramada,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>RN-11: el equipo tiene partidos en cualquier torneo.</summary>
    Task<bool> ExistsForEquipoAsync(Guid equipoId, CancellationToken cancellationToken = default);

    /// <summary>RN-20: el equipo tiene partidos en el torneo indicado.</summary>
    Task<bool> ExistsForEquipoInTorneoAsync(Guid torneoId, Guid equipoId, CancellationToken cancellationToken = default);

    /// <summary>El torneo tiene partidos jugados.</summary>
    Task<bool> ExistsPlayedInTorneoAsync(Guid torneoId, CancellationToken cancellationToken = default);

    void Add(Partido partido);
}
