using Dapper;
using MundialitoCorporativo.Application.Abstractions.Data;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Torneos;
using MundialitoCorporativo.Application.Torneos.Queries;
using MundialitoCorporativo.Infrastructure.Queries.Sql;

namespace MundialitoCorporativo.Infrastructure.Queries;

/// <summary>
/// Lectura de torneos, equipos inscritos, tabla de posiciones y goleadores con Dapper.
/// </summary>
internal sealed class TorneoQueries(IDbConnectionFactory connectionFactory) : ITorneoQueries
{
    private const string TorneoSelect = """
        t.Id, t.Nombre, t.Descripcion, t.FechaInicio, t.FechaFin, t.Estado,
        (SELECT COUNT(*)
           FROM dbo.TorneoEquipos te
           JOIN dbo.Equipos e ON e.Id = te.EquipoId AND e.Eliminado = 0
          WHERE te.TorneoId = t.Id) AS CantidadEquipos,
        (SELECT COUNT(*) FROM dbo.Partidos p WHERE p.TorneoId = t.Id AND p.Eliminado = 0) AS CantidadPartidos
        """;

    private const string TorneoFrom = "FROM dbo.Torneos t";

    private const string EquiposSelect = """
        te.EquipoId, e.Nombre AS Equipo, e.Abreviatura, e.Departamento,
        TODATETIMEOFFSET(te.FechaInscripcion, 0) AS FechaInscripcion
        """;

    private const string EquiposFrom = """
        FROM dbo.TorneoEquipos te
        JOIN dbo.Equipos e ON e.Id = te.EquipoId AND e.Eliminado = 0
        """;

    /// <summary>
    /// Tabla de posiciones (requerimiento ítem 8). Cada partido jugado se convierte en dos filas (una por equipo);
    /// se agregan por equipo inscrito y se numeran con el orden oficial: puntos, diferencia de gol, goles a favor
    /// (desempate final por nombre). Los goles se convierten a INT porque la diferencia puede ser negativa.
    /// </summary>
    private const string TablaCte = """
        WITH Resultados AS (
            SELECT p.EquipoLocalId AS EquipoId, CAST(p.GolesLocal AS INT) AS GF, CAST(p.GolesVisitante AS INT) AS GC
              FROM dbo.Partidos p
             WHERE p.TorneoId = @TorneoId AND p.Estado = 'Jugado' AND p.Eliminado = 0
            UNION ALL
            SELECT p.EquipoVisitanteId, CAST(p.GolesVisitante AS INT), CAST(p.GolesLocal AS INT)
              FROM dbo.Partidos p
             WHERE p.TorneoId = @TorneoId AND p.Estado = 'Jugado' AND p.Eliminado = 0
        ),
        Acumulado AS (
            SELECT e.Id AS EquipoId, e.Nombre AS Equipo, e.Abreviatura,
                   COUNT(r.EquipoId)                                                         AS PartidosJugados,
                   ISNULL(SUM(CASE WHEN r.GF > r.GC THEN 1 ELSE 0 END), 0)                   AS Ganados,
                   ISNULL(SUM(CASE WHEN r.GF = r.GC THEN 1 ELSE 0 END), 0)                   AS Empatados,
                   ISNULL(SUM(CASE WHEN r.GF < r.GC THEN 1 ELSE 0 END), 0)                   AS Perdidos,
                   ISNULL(SUM(r.GF), 0)                                                      AS GolesFavor,
                   ISNULL(SUM(r.GC), 0)                                                      AS GolesContra,
                   ISNULL(SUM(r.GF - r.GC), 0)                                               AS DiferenciaGol,
                   ISNULL(SUM(CASE WHEN r.GF > r.GC THEN 3 WHEN r.GF = r.GC THEN 1 ELSE 0 END), 0) AS Puntos
              FROM dbo.TorneoEquipos te
              JOIN dbo.Equipos e ON e.Id = te.EquipoId AND e.Eliminado = 0
              LEFT JOIN Resultados r ON r.EquipoId = te.EquipoId
             WHERE te.TorneoId = @TorneoId
             GROUP BY e.Id, e.Nombre, e.Abreviatura
        ),
        Tabla AS (
            SELECT ROW_NUMBER() OVER (ORDER BY Puntos DESC, DiferenciaGol DESC, GolesFavor DESC, Equipo ASC) AS Posicion,
                   EquipoId, Equipo, Abreviatura, PartidosJugados, Ganados, Empatados, Perdidos,
                   GolesFavor, GolesContra, DiferenciaGol, Puntos
              FROM Acumulado
        )
        """;

    private const string TablaSelect =
        "Posicion, EquipoId, Equipo, Abreviatura, PartidosJugados, Ganados, Empatados, Perdidos, GolesFavor, GolesContra, DiferenciaGol, Puntos";

    /// <summary>
    /// Goleadores del torneo: goles de partidos jugados agrupados por jugador. RANK hace que los empatados
    /// compartan posición.
    /// </summary>
    private const string GoleadoresCte = """
        WITH GolesPorJugador AS (
            SELECT g.JugadorId, g.EquipoId, COUNT(*) AS Goles, COUNT(DISTINCT g.PartidoId) AS PartidosConGol
              FROM dbo.Goles g
              JOIN dbo.Partidos p ON p.Id = g.PartidoId AND p.Estado = 'Jugado' AND p.Eliminado = 0
             WHERE p.TorneoId = @TorneoId
             GROUP BY g.JugadorId, g.EquipoId
        ),
        Ranking AS (
            SELECT RANK() OVER (ORDER BY gj.Goles DESC) AS Posicion,
                   gj.JugadorId, j.Nombres + N' ' + j.Apellidos AS Jugador, j.Nombres, j.Apellidos,
                   j.NumeroCamiseta, pj.Nombre AS PosicionJuego, gj.EquipoId, e.Nombre AS Equipo,
                   gj.Goles, gj.PartidosConGol
              FROM GolesPorJugador gj
              JOIN dbo.Jugadores j ON j.Id = gj.JugadorId
              JOIN dbo.PosicionesJuego pj ON pj.Id = j.PosicionJuegoId
              JOIN dbo.Equipos e ON e.Id = gj.EquipoId
        )
        """;

    private const string GoleadoresSelect =
        "Posicion, JugadorId, Jugador, NumeroCamiseta, PosicionJuego, EquipoId, Equipo, Goles, PartidosConGol";

    internal static readonly SqlSort TorneoSort = SqlSort.For(
        "t.Id",
        ("fechaInicio", "t.FechaInicio"),
        ("nombre", "t.Nombre"),
        ("estado", "t.Estado"));

    internal static readonly SqlSort EquiposSort = SqlSort.For(
        "te.EquipoId",
        ("equipo", "e.Nombre"),
        ("fechaInscripcion", "te.FechaInscripcion"));

    internal static readonly SqlSort TablaSort = SqlSort.For(
        "Posicion",
        ("posicion", "Posicion"),
        ("equipo", "Equipo"),
        ("puntos", "Puntos"),
        ("diferenciaGol", "DiferenciaGol"),
        ("golesFavor", "GolesFavor"));

    internal static readonly SqlSort GoleadoresSort = SqlSort.For(
        "Apellidos, JugadorId",
        ("posicion", "Posicion"),
        ("goles", "Goles"),
        ("jugador", "Apellidos"),
        ("equipo", "Equipo"));

    public Task<PagedResponse<TorneoResponse>> GetPagedAsync(GetTorneosQuery query, CancellationToken cancellationToken)
    {
        var filter = new SqlFilter("t.Eliminado = 0")
            .AddContains(query.Nombre, "t.Nombre LIKE @Nombre", "Nombre")
            .AddIf(query.Estado is not null, "t.Estado = @Estado", "Estado", () => query.Estado!.Value.ToString());

        return PagedSql.QueryAsync<TorneoResponse>(connectionFactory, query, TorneoSelect, TorneoFrom, filter, TorneoSort, cancellationToken);
    }

    public async Task<TorneoResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<TorneoResponse>(new CommandDefinition(
            $"SELECT {TorneoSelect} {TorneoFrom} WHERE t.Id = @Id AND t.Eliminado = 0",
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        PagedSql.ExistsAsync(connectionFactory, "dbo.Torneos", id, cancellationToken);

    public Task<PagedResponse<TorneoEquipoResponse>> GetEquiposPagedAsync(GetTorneoEquiposQuery query, CancellationToken cancellationToken)
    {
        var filter = new SqlFilter()
            .Add("te.TorneoId = @TorneoId", "TorneoId", query.TorneoId)
            .AddContains(query.Nombre, "(e.Nombre LIKE @Nombre OR e.Abreviatura LIKE @Nombre)", "Nombre");

        return PagedSql.QueryAsync<TorneoEquipoResponse>(connectionFactory, query, EquiposSelect, EquiposFrom, filter, EquiposSort, cancellationToken);
    }

    public Task<PagedResponse<FilaTablaPosicionesResponse>> GetTablaPosicionesAsync(GetTablaPosicionesQuery query, CancellationToken cancellationToken)
    {
        // El filtro por equipo se aplica después de numerar: la posición mostrada siempre es la oficial.
        var filter = new SqlFilter()
            .AddContains(query.Equipo, "(Equipo LIKE @Equipo OR Abreviatura LIKE @Equipo)", "Equipo");
        filter.Parameters.Add("TorneoId", query.TorneoId);

        return PagedSql.QueryAsync<FilaTablaPosicionesResponse>(
            connectionFactory, query, TablaSelect, "FROM Tabla", filter, TablaSort, cancellationToken, TablaCte);
    }

    public Task<PagedResponse<GoleadorResponse>> GetGoleadoresAsync(GetGoleadoresQuery query, CancellationToken cancellationToken)
    {
        var filter = new SqlFilter()
            .AddIf(query.EquipoId is not null, "EquipoId = @EquipoId", "EquipoId", () => query.EquipoId!.Value)
            .AddContains(query.Jugador, "(Nombres LIKE @Jugador OR Apellidos LIKE @Jugador)", "Jugador");
        filter.Parameters.Add("TorneoId", query.TorneoId);

        return PagedSql.QueryAsync<GoleadorResponse>(
            connectionFactory, query, GoleadoresSelect, "FROM Ranking", filter, GoleadoresSort, cancellationToken, GoleadoresCte);
    }
}
