using Dapper;
using MundialitoCorporativo.Application.Abstractions.Data;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Jugadores;
using MundialitoCorporativo.Application.Jugadores.Queries;
using MundialitoCorporativo.Infrastructure.Queries.Sql;

namespace MundialitoCorporativo.Infrastructure.Queries;

/// <summary>
/// Lectura de jugadores (integrantes de equipos) con Dapper.
/// </summary>
internal sealed class JugadorQueries(IDbConnectionFactory connectionFactory) : IJugadorQueries
{
    private const string Select = """
        j.Id, j.EquipoId, e.Nombre AS Equipo, j.PosicionJuegoId, pj.Nombre AS PosicionJuego,
        j.Nombres, j.Apellidos, j.NumeroCamiseta, j.Activo,
        (SELECT COUNT(*)
           FROM dbo.Goles g
           JOIN dbo.Partidos p ON p.Id = g.PartidoId AND p.Eliminado = 0
          WHERE g.JugadorId = j.Id) AS Goles
        """;

    private const string From = """
        FROM dbo.Jugadores j
        JOIN dbo.Equipos e ON e.Id = j.EquipoId AND e.Eliminado = 0
        JOIN dbo.PosicionesJuego pj ON pj.Id = j.PosicionJuegoId
        """;

    internal static readonly SqlSort Sort = SqlSort.For(
        "j.Id",
        ("apellidos", "j.Apellidos"),
        ("nombres", "j.Nombres"),
        ("numeroCamiseta", "j.NumeroCamiseta"),
        ("equipo", "e.Nombre"),
        ("posicionJuego", "pj.Orden"));

    public Task<PagedResponse<JugadorResponse>> GetPagedAsync(GetJugadoresQuery query, CancellationToken cancellationToken)
    {
        var filter = new SqlFilter("j.Eliminado = 0")
            .AddIf(query.EquipoId is not null, "j.EquipoId = @EquipoId", "EquipoId", () => query.EquipoId!.Value)
            .AddContains(query.Nombre, "(j.Nombres LIKE @Nombre OR j.Apellidos LIKE @Nombre)", "Nombre")
            .AddIf(query.PosicionJuegoId is not null, "j.PosicionJuegoId = @PosicionJuegoId", "PosicionJuegoId", () => query.PosicionJuegoId!.Value)
            .AddIf(query.Activo is not null, "j.Activo = @Activo", "Activo", () => query.Activo!.Value);

        return PagedSql.QueryAsync<JugadorResponse>(connectionFactory, query, Select, From, filter, Sort, cancellationToken);
    }

    public async Task<JugadorResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<JugadorResponse>(new CommandDefinition(
            $"SELECT {Select} {From} WHERE j.Id = @Id AND j.Eliminado = 0",
            new { Id = id },
            cancellationToken: cancellationToken));
    }
}
