using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MundialitoCorporativo.Application;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Torneos.Queries;
using MundialitoCorporativo.Infrastructure;
using MundialitoCorporativo.Infrastructure.Initialization;

namespace MundialitoCorporativo.IntegrationTests.Seed;

/// <summary>
/// Seed de datos (ítem 9) como en Docker: a partir de un servidor sin la base, la API crea la base y su esquema
/// y carga 4 equipos, 5 jugadores por equipo y 6 partidos (3 con resultado). La tabla queda lista de inmediato.
/// Si existe la carpeta local Db/, también se valida que el script 003 deja exactamente la misma tabla.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class DatabaseSeedSqlServerTests(SqlServerDatabaseFixture fixture) : IAsyncLifetime
{
    private static readonly FilaEsperada[] TablaEsperada =
    [
        new(1, "Operaciones City", 2, 1, 1, 0, 4, 1, 3, 4),
        new(2, "Tecnología FC", 2, 1, 0, 1, 2, 4, -2, 3),
        new(3, "Recursos Humanos Athletic", 1, 0, 1, 0, 1, 1, 0, 1),
        new(4, "Finanzas United", 1, 0, 0, 1, 1, 2, -1, 0)
    ];

    private readonly string _databaseName = $"Mundialito_Seed_{Guid.NewGuid():N}"[..32];

    private string ConnectionString =>
        new SqlConnectionStringBuilder(fixture.ServerConnectionString) { InitialCatalog = _databaseName }.ConnectionString;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (fixture.IsAvailable)
        {
            await SqlServerDatabaseFixture.DropDatabaseAsync(fixture.ServerConnectionString, _databaseName);
        }
    }

    [SkippableFact]
    public async Task InitializeDatabase_WithoutDatabase_ShouldCreateSchemaAndSeedRequiredData()
    {
        SkipIfUnavailable();
        await using var provider = BuildProvider();

        await provider.InitializeDatabaseAsync();

        Assert.Equal(1, await CountAsync("SELECT COUNT(*) FROM dbo.Torneos WHERE Estado = 'EnCurso'"));
        Assert.Equal(4, await CountAsync("SELECT COUNT(*) FROM dbo.Equipos"));
        Assert.Equal(4, await CountAsync("SELECT COUNT(*) FROM dbo.TorneoEquipos"));
        Assert.Equal(20, await CountAsync("SELECT COUNT(*) FROM dbo.Jugadores"));
        Assert.Equal(4, await CountAsync("SELECT COUNT(*) FROM (SELECT EquipoId FROM dbo.Jugadores GROUP BY EquipoId HAVING COUNT(*) = 5) AS e"));
        Assert.Equal(6, await CountAsync("SELECT COUNT(*) FROM dbo.Partidos"));
        Assert.Equal(3, await CountAsync("SELECT COUNT(*) FROM dbo.Partidos WHERE Estado = 'Jugado' AND FechaProgramada < SYSUTCDATETIME()"));
        Assert.Equal(3, await CountAsync("SELECT COUNT(*) FROM dbo.Partidos WHERE Estado = 'Programado' AND FechaProgramada > SYSUTCDATETIME()"));
        Assert.Equal(8, await CountAsync("SELECT COUNT(*) FROM dbo.Goles"));
        Assert.Equal(2, await CountAsync("SELECT COUNT(*) FROM sys.views WHERE name IN ('vw_TablaPosiciones', 'vw_Goleadores')"));

        // El seed usa el dominio y el Unit of Work: los eventos quedan registrados y procesados.
        Assert.Equal(4, await CountAsync("SELECT COUNT(*) FROM dbo.EventosDominio WHERE TipoEvento = 'EquipoCreatedDomainEvent'"));
        Assert.Equal(3, await CountAsync("SELECT COUNT(*) FROM dbo.EventosDominio WHERE TipoEvento = 'PartidoResultRegisteredDomainEvent'"));
        Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM dbo.EventosDominio WHERE FechaProcesado IS NULL"));

        await AssertTablaEsperadaAsync(provider);
    }

    [SkippableFact]
    public async Task InitializeDatabase_RunAgain_ShouldNotDuplicateData()
    {
        SkipIfUnavailable();
        await using (var first = BuildProvider())
        {
            await first.InitializeDatabaseAsync();
        }

        await using var second = BuildProvider();
        await second.InitializeDatabaseAsync();

        Assert.Equal(1, await CountAsync("SELECT COUNT(*) FROM dbo.Torneos"));
        Assert.Equal(4, await CountAsync("SELECT COUNT(*) FROM dbo.Equipos"));
        Assert.Equal(20, await CountAsync("SELECT COUNT(*) FROM dbo.Jugadores"));
        Assert.Equal(6, await CountAsync("SELECT COUNT(*) FROM dbo.Partidos"));
    }

    [SkippableFact]
    public async Task SeedScript_ShouldLoadSameDataAndBeReexecutable()
    {
        SkipIfUnavailable();
        var seedScriptPath = SqlServerDatabaseFixture.FindDbScript("003_DML_SeedInicial.sql");
        Skip.If(seedScriptPath is null, "La carpeta local Db/ no está disponible (no forma parte del repositorio).");

        await SqlServerDatabaseFixture.RunScriptAsync(fixture.ServerConnectionString, SqlScripts.Load(SqlScripts.CrearBaseDatos), _databaseName);
        var seedScript = await File.ReadAllTextAsync(seedScriptPath);

        await SqlServerDatabaseFixture.RunScriptAsync(fixture.ServerConnectionString, seedScript, _databaseName);
        await SqlServerDatabaseFixture.RunScriptAsync(fixture.ServerConnectionString, seedScript, _databaseName);

        Assert.Equal(1, await CountAsync("SELECT COUNT(*) FROM dbo.Torneos"));
        Assert.Equal(4, await CountAsync("SELECT COUNT(*) FROM dbo.Equipos"));
        Assert.Equal(20, await CountAsync("SELECT COUNT(*) FROM dbo.Jugadores"));
        Assert.Equal(6, await CountAsync("SELECT COUNT(*) FROM dbo.Partidos"));
        Assert.Equal(8, await CountAsync("SELECT COUNT(*) FROM dbo.Goles"));

        await using var provider = BuildProvider();
        await AssertTablaEsperadaAsync(provider);
    }

    private async Task AssertTablaEsperadaAsync(IServiceProvider provider)
    {
        var torneoId = await ScalarAsync<Guid>("SELECT Id FROM dbo.Torneos");
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var tabla = await dispatcher.Query(new GetTablaPosicionesQuery { TorneoId = torneoId });
        Assert.True(tabla.IsSuccess, tabla.ErrorMessage);
        var filas = tabla.Value.Data
            .Select(fila => new FilaEsperada(fila.Posicion, fila.Equipo, fila.PartidosJugados, fila.Ganados, fila.Empatados,
                fila.Perdidos, fila.GolesFavor, fila.GolesContra, fila.DiferenciaGol, fila.Puntos))
            .ToArray();
        Assert.Equal(TablaEsperada, filas);

        var goleadores = await dispatcher.Query(new GetGoleadoresQuery { TorneoId = torneoId });
        Assert.True(goleadores.IsSuccess, goleadores.ErrorMessage);
        var lider = goleadores.Value.Data[0];
        Assert.Equal(("Alejandro Morales", 3), (lider.Jugador, lider.Goles));
        Assert.Equal(8, goleadores.Value.Data.Sum(goleador => goleador.Goles));
    }

    private ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString,
                ["Database:ConnectionRetries"] = "1"
            })
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider(validateScopes: true);
    }

    private async Task<int> CountAsync(string sql) => await ScalarAsync<int>(sql);

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private void SkipIfUnavailable() => Skip.IfNot(fixture.IsAvailable, fixture.UnavailableReason);

    private sealed record FilaEsperada(int Posicion, string Equipo, int PJ, int G, int E, int P, int GF, int GC, int DG, int Pts);
}
