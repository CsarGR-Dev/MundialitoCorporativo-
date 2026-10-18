using Microsoft.EntityFrameworkCore;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.PosicionesJuego;

namespace MundialitoCorporativo.Infrastructure.Persistence.Repositories;

internal sealed class PosicionJuegoRepository(MundialitoDbContext dbContext) : IPosicionJuegoRepository
{
    public Task<PosicionJuego?> GetByIdAsync(byte id, CancellationToken cancellationToken = default) =>
        dbContext.PosicionesJuego.AsNoTracking().FirstOrDefaultAsync(posicion => posicion.Id == id, cancellationToken);
}
