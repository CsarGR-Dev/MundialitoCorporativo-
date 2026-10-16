using Microsoft.EntityFrameworkCore;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Partidos;

namespace MundialitoCorporativo.Infrastructure.Persistence.Repositories;

internal sealed class PartidoRepository(MundialitoDbContext dbContext) : IPartidoRepository
{
    public Task<Partido?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Partidos
            .Include(partido => partido.Goles)
            .FirstOrDefaultAsync(partido => partido.Id == id, cancellationToken);

    public Task<Partido?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Partidos
            .IgnoreQueryFilters()
            .Include(partido => partido.Goles)
            .FirstOrDefaultAsync(partido => partido.Id == id, cancellationToken);

    public Task<bool> ExistsAtSameTimeAsync(
        Guid equipoLocalId,
        Guid equipoVisitanteId,
        DateTime fechaProgramada,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Partidos.AnyAsync(
            partido => partido.FechaProgramada == fechaProgramada
                       && partido.Estado != EstadoPartido.Cancelado
                       && (excludeId == null || partido.Id != excludeId)
                       && (partido.EquipoLocalId == equipoLocalId
                           || partido.EquipoVisitanteId == equipoLocalId
                           || partido.EquipoLocalId == equipoVisitanteId
                           || partido.EquipoVisitanteId == equipoVisitanteId),
            cancellationToken);

    public Task<bool> ExistsForEquipoAsync(Guid equipoId, CancellationToken cancellationToken = default) =>
        dbContext.Partidos.AnyAsync(
            partido => partido.EquipoLocalId == equipoId || partido.EquipoVisitanteId == equipoId,
            cancellationToken);

    public Task<bool> ExistsForEquipoInTorneoAsync(Guid torneoId, Guid equipoId, CancellationToken cancellationToken = default) =>
        dbContext.Partidos.IgnoreQueryFilters().AnyAsync(
            partido => partido.TorneoId == torneoId
                       && (partido.EquipoLocalId == equipoId || partido.EquipoVisitanteId == equipoId),
            cancellationToken);

    public Task<bool> ExistsPlayedInTorneoAsync(Guid torneoId, CancellationToken cancellationToken = default) =>
        dbContext.Partidos.AnyAsync(
            partido => partido.TorneoId == torneoId && partido.Estado == EstadoPartido.Jugado,
            cancellationToken);

    public void Add(Partido partido) => dbContext.Partidos.Add(partido);
}
