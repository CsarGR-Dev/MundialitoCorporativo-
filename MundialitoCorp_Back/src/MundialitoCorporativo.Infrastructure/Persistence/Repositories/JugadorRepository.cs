using Microsoft.EntityFrameworkCore;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Jugadores;

namespace MundialitoCorporativo.Infrastructure.Persistence.Repositories;

internal sealed class JugadorRepository(MundialitoDbContext dbContext) : IJugadorRepository
{
    public Task<Jugador?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Jugadores.FirstOrDefaultAsync(jugador => jugador.Id == id, cancellationToken);

    public Task<Jugador?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Jugadores.IgnoreQueryFilters().FirstOrDefaultAsync(jugador => jugador.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Jugador>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var distinctIds = ids.Distinct().ToList();

        return await dbContext.Jugadores
            .Where(jugador => distinctIds.Contains(jugador.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsNumeroCamisetaAsync(Guid equipoId, int numeroCamiseta, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        dbContext.Jugadores.AnyAsync(
            jugador => jugador.EquipoId == equipoId
                       && jugador.NumeroCamiseta == numeroCamiseta
                       && (excludeId == null || jugador.Id != excludeId),
            cancellationToken);

    public void Add(Jugador jugador) => dbContext.Jugadores.Add(jugador);
}
