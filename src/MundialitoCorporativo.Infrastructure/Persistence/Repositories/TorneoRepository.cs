using Microsoft.EntityFrameworkCore;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Infrastructure.Persistence.Repositories;

internal sealed class TorneoRepository(MundialitoDbContext dbContext) : ITorneoRepository
{
    public Task<Torneo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Torneos
            .Include(torneo => torneo.Equipos)
            .FirstOrDefaultAsync(torneo => torneo.Id == id, cancellationToken);

    public Task<Torneo?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Torneos
            .IgnoreQueryFilters()
            .Include(torneo => torneo.Equipos)
            .FirstOrDefaultAsync(torneo => torneo.Id == id, cancellationToken);

    public Task<bool> ExistsByNombreAsync(string nombre, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        dbContext.Torneos.AnyAsync(
            torneo => torneo.Nombre == nombre.Trim() && (excludeId == null || torneo.Id != excludeId),
            cancellationToken);

    public void Add(Torneo torneo) => dbContext.Torneos.Add(torneo);
}
