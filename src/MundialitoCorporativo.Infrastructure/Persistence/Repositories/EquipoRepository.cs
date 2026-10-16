using Microsoft.EntityFrameworkCore;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Equipos;

namespace MundialitoCorporativo.Infrastructure.Persistence.Repositories;

internal sealed class EquipoRepository(MundialitoDbContext dbContext) : IEquipoRepository
{
    public Task<Equipo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Equipos.FirstOrDefaultAsync(equipo => equipo.Id == id, cancellationToken);

    public Task<Equipo?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Equipos.IgnoreQueryFilters().FirstOrDefaultAsync(equipo => equipo.Id == id, cancellationToken);

    // La intercalación de la base (Modern_Spanish_CI_AS) compara sin distinguir mayúsculas (RN-01).
    public Task<bool> ExistsByNombreAsync(string nombre, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        dbContext.Equipos.AnyAsync(
            equipo => equipo.Nombre == nombre.Trim() && (excludeId == null || equipo.Id != excludeId),
            cancellationToken);

    public Task<bool> ExistsByAbreviaturaAsync(string abreviatura, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = Equipo.NormalizeAbreviatura(abreviatura);

        return dbContext.Equipos.AnyAsync(
            equipo => equipo.Abreviatura == normalized && (excludeId == null || equipo.Id != excludeId),
            cancellationToken);
    }

    public void Add(Equipo equipo) => dbContext.Equipos.Add(equipo);
}
