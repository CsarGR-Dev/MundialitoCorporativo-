using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Torneos;
using MundialitoCorporativo.Application.Torneos.Queries;

namespace MundialitoCorporativo.Application.Abstractions.Queries;

/// <summary>
/// Lectura de torneos, equipos inscritos, tabla de posiciones y goleadores. Implementada con Dapper en Infrastructure.
/// </summary>
public interface ITorneoQueries
{
    Task<PagedResponse<TorneoResponse>> GetPagedAsync(GetTorneosQuery query, CancellationToken cancellationToken);

    Task<TorneoResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResponse<TorneoEquipoResponse>> GetEquiposPagedAsync(GetTorneoEquiposQuery query, CancellationToken cancellationToken);

    /// <summary>Tabla de posiciones calculada en SQL a partir de los partidos jugados (requerimiento ítem 8).</summary>
    Task<PagedResponse<FilaTablaPosicionesResponse>> GetTablaPosicionesAsync(GetTablaPosicionesQuery query, CancellationToken cancellationToken);

    Task<PagedResponse<GoleadorResponse>> GetGoleadoresAsync(GetGoleadoresQuery query, CancellationToken cancellationToken);
}
