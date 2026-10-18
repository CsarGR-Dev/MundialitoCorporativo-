using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Equipos.Commands;
using MundialitoCorporativo.Application.Equipos.Queries;
using MundialitoCorporativo.Application.Jugadores.Commands;
using MundialitoCorporativo.Application.Jugadores.Queries;
using MundialitoCorporativo.Application.Partidos.Commands;
using MundialitoCorporativo.Application.Partidos.Queries;
using MundialitoCorporativo.Application.PosicionesJuego.Queries;
using MundialitoCorporativo.Application.Torneos.Queries;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Partidos;

namespace MundialitoCorporativo.IntegrationTests.Cqrs;

/// <summary>
/// Commands (EF Core + Unit of Work) y Queries (Dapper) contra SQL Server, a través del despachador.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class CqrsSqlServerTests(SqlServerDatabaseFixture fixture) : IDisposable
{
    private static int _scenarioCount;
    private readonly CqrsHost? _host = fixture.IsAvailable ? new CqrsHost(fixture.ConnectionString) : null;

    private CqrsHost Host => _host!;

    public void Dispose() => _host?.Dispose();

    [SkippableFact]
    public async Task TablaPosiciones_WithSeedScenario_ShouldFollowRequiredOrder()
    {
        var scenario = await CreateScenarioAsync();

        var result = await Host.Query(new GetTablaPosicionesQuery { TorneoId = scenario.TorneoId });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(4, result.Value.TotalRecords);
        Assert.Collection(
            result.Value.Data,
            fila => AssertFila(fila, 1, scenario.Operaciones, pj: 2, puntos: 4, dg: 3, gf: 4),
            fila => AssertFila(fila, 2, scenario.Tecnologia, pj: 2, puntos: 3, dg: -2, gf: 2),
            fila => AssertFila(fila, 3, scenario.RecursosHumanos, pj: 1, puntos: 1, dg: 0, gf: 1),
            fila => AssertFila(fila, 4, scenario.Finanzas, pj: 1, puntos: 0, dg: -1, gf: 1));
    }

    [SkippableFact]
    public async Task TablaPosiciones_FilteredByEquipo_ShouldKeepOfficialPosition()
    {
        var scenario = await CreateScenarioAsync();

        var result = await Host.Query(new GetTablaPosicionesQuery { TorneoId = scenario.TorneoId, Equipo = "Finanzas" });

        var fila = Assert.Single(result.Value.Data);
        Assert.Equal(4, fila.Posicion);
    }

    [SkippableFact]
    public async Task TablaPosiciones_ForUnknownTorneo_ShouldReturnNotFound()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.UnavailableReason);

        var result = await Host.Query(new GetTablaPosicionesQuery { TorneoId = Guid.NewGuid() });

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [SkippableFact]
    public async Task Goleadores_ShouldRankByGolesAndShareTiedPositions()
    {
        var scenario = await CreateScenarioAsync();

        var result = await Host.Query(new GetGoleadoresQuery { TorneoId = scenario.TorneoId });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var goleadores = result.Value.Data;
        Assert.Equal(5, result.Value.TotalRecords);
        Assert.Equal((1, scenario.Ramos, 3, 2), (goleadores[0].Posicion, goleadores[0].JugadorId, goleadores[0].Goles, goleadores[0].PartidosConGol));
        Assert.Equal((2, scenario.Lopez, 2), (goleadores[1].Posicion, goleadores[1].JugadorId, goleadores[1].Goles));
        Assert.All(goleadores.Skip(2), goleador => Assert.Equal((3, 1), (goleador.Posicion, goleador.Goles)));
    }

    [SkippableFact]
    public async Task Partidos_ShouldFilterByTorneoEstadoFechaYEquipo()
    {
        var scenario = await CreateScenarioAsync();

        var jugados = await Host.Query(new GetPartidosQuery { TorneoId = scenario.TorneoId, Estado = EstadoPartido.Jugado });
        var programados = await Host.Query(new GetPartidosQuery { TorneoId = scenario.TorneoId, Estado = EstadoPartido.Programado });
        var deTecnologia = await Host.Query(new GetPartidosQuery { TorneoId = scenario.TorneoId, EquipoId = scenario.Tecnologia });

        var primerDia = (await Host.Query(new GetPartidosQuery { TorneoId = scenario.TorneoId })).Value.Data[0].FechaProgramada;
        var dosDias = await Host.Query(new GetPartidosQuery
        {
            TorneoId = scenario.TorneoId,
            FechaDesde = DateOnly.FromDateTime(primerDia.UtcDateTime),
            FechaHasta = DateOnly.FromDateTime(primerDia.UtcDateTime.AddDays(1))
        });

        Assert.Equal(3, jugados.Value.TotalRecords);
        Assert.All(jugados.Value.Data, partido => Assert.Equal("Jugado", partido.Estado));
        Assert.Equal(3, programados.Value.TotalRecords);
        Assert.Equal(3, deTecnologia.Value.TotalRecords);
        Assert.Equal(2, dosDias.Value.TotalRecords);
        Assert.Equal(TimeSpan.Zero, primerDia.Offset);
    }

    [SkippableFact]
    public async Task PartidoDetalle_ShouldIncludeGolesOrderedByMinuto()
    {
        var scenario = await CreateScenarioAsync();

        var result = await Host.Query(new GetPartidoByIdQuery(scenario.PartidoTecnologiaFinanzas));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal((2, 1), (result.Value.GolesLocal, result.Value.GolesVisitante));
        Assert.Equal([10, 40, 70], result.Value.Goles.Select(gol => (int)gol.Minuto));
        Assert.Equal("Ana López", result.Value.Goles[0].Jugador);
    }

    [SkippableFact]
    public async Task Equipos_ShouldPaginateInDatabaseWithRequiredResponseFormat()
    {
        var scenario = await CreateScenarioAsync();

        var page2 = await Host.Query(new GetEquiposQuery { Nombre = scenario.Sufijo, PageNumber = 2, PageSize = 3, SortBy = "nombre" });
        var outOfRange = await Host.Query(new GetEquiposQuery { Nombre = scenario.Sufijo, PageNumber = 5, PageSize = 3 });
        var descending = await Host.Query(new GetEquiposQuery { Nombre = scenario.Sufijo, SortBy = "nombre", SortDirection = SortDirection.Desc });

        Assert.Equal((2, 3, 4, 2), (page2.Value.PageNumber, page2.Value.PageSize, page2.Value.TotalRecords, page2.Value.TotalPages));
        Assert.Single(page2.Value.Data);
        Assert.Empty(outOfRange.Value.Data);
        Assert.Equal(4, outOfRange.Value.TotalRecords);
        Assert.StartsWith("Tecnología FC", descending.Value.Data[0].Nombre, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task EveryAllowedSortField_ShouldExecuteInBothDirections()
    {
        var scenario = await CreateScenarioAsync();

        foreach (var direction in new[] { SortDirection.Asc, SortDirection.Desc })
        {
            foreach (var field in GetEquiposQuery.SortFields)
                AssertSuccess(await Host.Query(new GetEquiposQuery { SortBy = field, SortDirection = direction }), field);
            foreach (var field in GetJugadoresQuery.SortFields)
                AssertSuccess(await Host.Query(new GetJugadoresQuery { SortBy = field, SortDirection = direction }), field);
            foreach (var field in GetPartidosQuery.SortFields)
                AssertSuccess(await Host.Query(new GetPartidosQuery { SortBy = field, SortDirection = direction }), field);
            foreach (var field in GetTorneosQuery.SortFields)
                AssertSuccess(await Host.Query(new GetTorneosQuery { SortBy = field, SortDirection = direction }), field);
            foreach (var field in GetTorneoEquiposQuery.SortFields)
                AssertSuccess(await Host.Query(new GetTorneoEquiposQuery { TorneoId = scenario.TorneoId, SortBy = field, SortDirection = direction }), field);
            foreach (var field in GetTablaPosicionesQuery.SortFields)
                AssertSuccess(await Host.Query(new GetTablaPosicionesQuery { TorneoId = scenario.TorneoId, SortBy = field, SortDirection = direction }), field);
            foreach (var field in GetGoleadoresQuery.SortFields)
                AssertSuccess(await Host.Query(new GetGoleadoresQuery { TorneoId = scenario.TorneoId, SortBy = field, SortDirection = direction }), field);
            foreach (var field in GetPosicionesJuegoQuery.SortFields)
                AssertSuccess(await Host.Query(new GetPosicionesJuegoQuery { SortBy = field, SortDirection = direction }), field);
        }
    }

    [SkippableFact]
    public async Task TorneoDetalle_ShouldMapDatesAndCounts()
    {
        var scenario = await CreateScenarioAsync();

        var result = await Host.Query(new GetTorneoByIdQuery(scenario.TorneoId));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(new DateOnly(2026, 1, 1), result.Value.FechaInicio);
        Assert.Equal((4, 6), (result.Value.CantidadEquipos, result.Value.CantidadPartidos));
        Assert.Equal("Planificado", result.Value.Estado);
    }

    [SkippableFact]
    public async Task Jugadores_OfUnknownEquipo_ShouldReturnNotFound_AndFilterByNombre()
    {
        var scenario = await CreateScenarioAsync();

        var unknown = await Host.Query(new GetJugadoresQuery { EquipoId = Guid.NewGuid(), RequireEquipo = true });
        var ofOperaciones = await Host.Query(new GetJugadoresQuery { EquipoId = scenario.Operaciones, RequireEquipo = true, Nombre = "ram" });

        Assert.Equal(ErrorType.NotFound, unknown.Error.Type);
        var ramos = Assert.Single(ofOperaciones.Value.Data);
        Assert.Equal((3, "Delantero"), (ramos.Goles, ramos.PosicionJuego));
    }

    [SkippableFact]
    public async Task Commands_ShouldEnforceBusinessRulesAndIdempotentDelete()
    {
        var scenario = await CreateScenarioAsync();

        var duplicate = await Host.Send(new CreateEquipoCommand($"TECNOLOGÍA FC {scenario.Sufijo}".ToUpperInvariant(), "ZZZZ", null));
        var deleteWithPartidos = await Host.Send(new DeleteEquipoCommand(scenario.Tecnologia));
        var resultTwice = await Host.Send(new RegisterPartidoResultCommand(scenario.PartidoTecnologiaFinanzas, 0, 0, []));
        var firstDelete = await Host.Send(new DeleteJugadorCommand(scenario.Lopez));
        var secondDelete = await Host.Send(new DeleteJugadorCommand(scenario.Lopez));
        var neverExisted = await Host.Send(new DeleteJugadorCommand(Guid.NewGuid()));

        Assert.Equal("Equipo.DuplicateNombre", duplicate.ErrorCode);
        Assert.Equal(EquipoErrors.HasPartidos, deleteWithPartidos.Error);
        Assert.Equal(PartidoErrors.AlreadyPlayed, resultTwice.Error);
        Assert.True(firstDelete.IsSuccess);
        Assert.True(secondDelete.IsSuccess);
        Assert.Equal(ErrorType.NotFound, neverExisted.Error.Type);
    }

    private async Task<TorneoScenario> CreateScenarioAsync()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.UnavailableReason);

        // Cada escenario usa días distintos para no chocar con la regla de un partido por equipo y horario.
        return await TorneoScenario.CreateAsync(Host, Interlocked.Increment(ref _scenarioCount) * 40);
    }

    private static void AssertSuccess<T>(Result<T> result, string field) =>
        Assert.True(result.IsSuccess, $"sortBy '{field}': {result.ErrorMessage}");

    private static void AssertFila(Application.Torneos.FilaTablaPosicionesResponse fila, int posicion, Guid equipoId, int pj, int puntos, int dg, int gf)
    {
        Assert.Equal(posicion, fila.Posicion);
        Assert.Equal(equipoId, fila.EquipoId);
        Assert.Equal(pj, fila.PartidosJugados);
        Assert.Equal(puntos, fila.Puntos);
        Assert.Equal(dg, fila.DiferenciaGol);
        Assert.Equal(gf, fila.GolesFavor);
    }
}
