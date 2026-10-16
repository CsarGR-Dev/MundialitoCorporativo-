using MundialitoCorporativo.Application.Abstractions.Data;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.PosicionesJuego;
using MundialitoCorporativo.Application.PosicionesJuego.Queries;
using MundialitoCorporativo.Infrastructure.Queries.Sql;

namespace MundialitoCorporativo.Infrastructure.Queries;

/// <summary>
/// Lectura del catálogo de posiciones de juego con Dapper.
/// </summary>
internal sealed class PosicionJuegoQueries(IDbConnectionFactory connectionFactory) : IPosicionJuegoQueries
{
    internal static readonly SqlSort Sort = SqlSort.For(
        "pj.Id",
        ("orden", "pj.Orden"),
        ("nombre", "pj.Nombre"));

    public Task<PagedResponse<PosicionJuegoResponse>> GetPagedAsync(GetPosicionesJuegoQuery query, CancellationToken cancellationToken)
    {
        var filter = new SqlFilter()
            .AddIf(query.Activo is not null, "pj.Activo = @Activo", "Activo", () => query.Activo!.Value);

        return PagedSql.QueryAsync<PosicionJuegoResponse>(
            connectionFactory,
            query,
            "pj.Id, pj.Nombre, pj.Abreviatura, pj.Orden, pj.Activo",
            "FROM dbo.PosicionesJuego pj",
            filter,
            Sort,
            cancellationToken);
    }
}
