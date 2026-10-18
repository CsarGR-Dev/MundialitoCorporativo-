using Dapper;
using MundialitoCorporativo.Application.Abstractions.Data;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Partidos;
using MundialitoCorporativo.Application.Partidos.Queries;
using MundialitoCorporativo.Infrastructure.Queries.Sql;

namespace MundialitoCorporativo.Infrastructure.Queries;

/// <summary>
/// Lectura de partidos (calendario, resultados y goles) con Dapper.
/// </summary>
internal sealed class PartidoQueries(IDbConnectionFactory connectionFactory) : IPartidoQueries
{
    private const string Select = """
        p.Id, p.TorneoId, t.Nombre AS Torneo,
        p.EquipoLocalId, el.Nombre AS EquipoLocal,
        p.EquipoVisitanteId, ev.Nombre AS EquipoVisitante,
        TODATETIMEOFFSET(p.FechaProgramada, 0) AS FechaProgramada, p.Sede, p.Estado,
        CAST(p.GolesLocal AS INT) AS GolesLocal, CAST(p.GolesVisitante AS INT) AS GolesVisitante,
        TODATETIMEOFFSET(p.FechaJugado, 0) AS FechaJugado
        """;

    private const string From = """
        FROM dbo.Partidos p
        JOIN dbo.Torneos t ON t.Id = p.TorneoId AND t.Eliminado = 0
        JOIN dbo.Equipos el ON el.Id = p.EquipoLocalId
        JOIN dbo.Equipos ev ON ev.Id = p.EquipoVisitanteId
        """;

    private const string GolesSql = """
        SELECT g.Id, g.JugadorId, j.Nombres + N' ' + j.Apellidos AS Jugador, g.EquipoId, e.Nombre AS Equipo, g.Minuto
          FROM dbo.Goles g
          JOIN dbo.Jugadores j ON j.Id = g.JugadorId
          JOIN dbo.Equipos e ON e.Id = g.EquipoId
         WHERE g.PartidoId = @Id
         ORDER BY g.Minuto, g.Id;
        """;

    internal static readonly SqlSort Sort = SqlSort.For(
        "p.Id",
        ("fechaProgramada", "p.FechaProgramada"),
        ("estado", "p.Estado"),
        ("torneo", "t.Nombre"));

    public Task<PagedResponse<PartidoResponse>> GetPagedAsync(GetPartidosQuery query, CancellationToken cancellationToken)
    {
        var filter = new SqlFilter("p.Eliminado = 0")
            .AddIf(query.TorneoId is not null, "p.TorneoId = @TorneoId", "TorneoId", () => query.TorneoId!.Value)
            .AddIf(query.FechaDesde is not null, "p.FechaProgramada >= @FechaDesde", "FechaDesde",
                () => query.FechaDesde!.Value.ToDateTime(TimeOnly.MinValue))
            .AddIf(query.FechaHasta is not null, "p.FechaProgramada < @FechaHastaExclusiva", "FechaHastaExclusiva",
                () => query.FechaHasta!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue))
            .AddIf(query.EquipoId is not null, "(p.EquipoLocalId = @EquipoId OR p.EquipoVisitanteId = @EquipoId)", "EquipoId",
                () => query.EquipoId!.Value)
            .AddIf(query.Estado is not null, "p.Estado = @Estado", "Estado", () => query.Estado!.Value.ToString());

        return PagedSql.QueryAsync<PartidoResponse>(connectionFactory, query, Select, From, filter, Sort, cancellationToken);
    }

    public async Task<PartidoDetalleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var results = await connection.QueryMultipleAsync(new CommandDefinition(
            $"SELECT {Select} {From} WHERE p.Id = @Id AND p.Eliminado = 0; {GolesSql}",
            new { Id = id },
            cancellationToken: cancellationToken));

        var partido = await results.ReadSingleOrDefaultAsync<PartidoDetalleResponse>();
        if (partido is null)
        {
            return null;
        }

        var goles = (await results.ReadAsync<GolResponse>()).AsList();
        return partido with { Goles = goles };
    }
}
