using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.PosicionesJuego;
using MundialitoCorporativo.Application.PosicionesJuego.Queries;

namespace MundialitoCorporativo.Application.Abstractions.Queries;

/// <summary>
/// Lectura del catálogo de posiciones de juego. Implementada con Dapper en Infrastructure.
/// </summary>
public interface IPosicionJuegoQueries
{
    Task<PagedResponse<PosicionJuegoResponse>> GetPagedAsync(GetPosicionesJuegoQuery query, CancellationToken cancellationToken);
}
