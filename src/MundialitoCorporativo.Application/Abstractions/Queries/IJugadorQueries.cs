using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Jugadores;
using MundialitoCorporativo.Application.Jugadores.Queries;

namespace MundialitoCorporativo.Application.Abstractions.Queries;

/// <summary>
/// Lectura de jugadores. Implementada con Dapper en Infrastructure.
/// </summary>
public interface IJugadorQueries
{
    Task<PagedResponse<JugadorResponse>> GetPagedAsync(GetJugadoresQuery query, CancellationToken cancellationToken);

    Task<JugadorResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
