using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Equipos;
using MundialitoCorporativo.Application.Equipos.Queries;

namespace MundialitoCorporativo.Application.Abstractions.Queries;

/// <summary>
/// Lectura de equipos. Implementada con Dapper en Infrastructure (requerimiento ítem 3: Queries sin EF Core).
/// </summary>
public interface IEquipoQueries
{
    Task<PagedResponse<EquipoResponse>> GetPagedAsync(GetEquiposQuery query, CancellationToken cancellationToken);

    Task<EquipoResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
