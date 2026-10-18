using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Partidos;
using MundialitoCorporativo.Application.Partidos.Queries;

namespace MundialitoCorporativo.Application.Abstractions.Queries;

/// <summary>
/// Lectura de partidos (calendario y resultados). Implementada con Dapper en Infrastructure.
/// </summary>
public interface IPartidoQueries
{
    Task<PagedResponse<PartidoResponse>> GetPagedAsync(GetPartidosQuery query, CancellationToken cancellationToken);

    Task<PartidoDetalleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
