using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MundialitoCorporativo.Application;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Equipos.Commands;
using MundialitoCorporativo.Application.Jugadores.Commands;
using MundialitoCorporativo.Application.Partidos.Commands;
using MundialitoCorporativo.Application.Torneos.Commands;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Partidos;
using MundialitoCorporativo.Infrastructure;

namespace MundialitoCorporativo.IntegrationTests.Cqrs;

/// <summary>
/// Pila completa (Application + Infrastructure) contra la base temporal. Cada operación usa su propio scope,
/// como ocurre con cada request HTTP.
/// </summary>
internal sealed class CqrsHost : IDisposable
{
    private readonly ServiceProvider _provider;

    public CqrsHost(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = connectionString })
            .Build();

        _provider = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider(validateScopes: true);
    }

    public async Task<Result> Send(ICommand command)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }

    public async Task<Result<T>> Send<T>(ICommand<T> command)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }

    public async Task<Result<T>> Query<T>(IQuery<T> query)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(query);
    }

    public void Dispose() => _provider.Dispose();
}

/// <summary>
/// Escenario equivalente al seed del requerimiento (ítem 9): 1 torneo, 4 equipos inscritos, 6 partidos y 3 resultados.
/// Los nombres llevan un sufijo único para que los escenarios de distintas pruebas no se mezclen.
/// </summary>
internal sealed class TorneoScenario
{
    private TorneoScenario(string sufijo)
    {
        Sufijo = sufijo;
    }

    public string Sufijo { get; }

    public Guid TorneoId { get; private set; }

    public Guid Tecnologia { get; private set; }

    public Guid Finanzas { get; private set; }

    public Guid Operaciones { get; private set; }

    public Guid RecursosHumanos { get; private set; }

    public Guid Lopez { get; private set; }

    public Guid Ramos { get; private set; }

    public Guid PartidoTecnologiaFinanzas { get; private set; }

    public static async Task<TorneoScenario> CreateAsync(CqrsHost host, int dayOffset)
    {
        var scenario = new TorneoScenario(Guid.NewGuid().ToString("N")[..6]);
        var s = scenario.Sufijo;

        scenario.TorneoId = Value(await host.Send(new CreateTorneoCommand($"Mundialito {s}", null, new DateOnly(2026, 1, 1), null)));
        scenario.Tecnologia = await CreateEquipoAsync(host, $"Tecnología FC {s}", UniqueTestData.Abreviatura('T'));
        scenario.Finanzas = await CreateEquipoAsync(host, $"Finanzas United {s}", UniqueTestData.Abreviatura('F'));
        scenario.Operaciones = await CreateEquipoAsync(host, $"Operaciones City {s}", UniqueTestData.Abreviatura('O'));
        scenario.RecursosHumanos = await CreateEquipoAsync(host, $"Recursos Humanos Athletic {s}", UniqueTestData.Abreviatura('R'));

        foreach (var equipo in new[] { scenario.Tecnologia, scenario.Finanzas, scenario.Operaciones, scenario.RecursosHumanos })
        {
            Ok(await host.Send(new RegisterEquipoInTorneoCommand(scenario.TorneoId, equipo)));
        }

        scenario.Lopez = Value(await host.Send(new RegisterJugadorCommand(scenario.Tecnologia, 4, "Ana", "López", 9)));
        var perez = Value(await host.Send(new RegisterJugadorCommand(scenario.Finanzas, 4, "Luis", "Pérez", 9)));
        scenario.Ramos = Value(await host.Send(new RegisterJugadorCommand(scenario.Operaciones, 4, "Sofía", "Ramos", 9)));
        var cruz = Value(await host.Send(new RegisterJugadorCommand(scenario.Operaciones, 3, "Iván", "Cruz", 8)));
        var mejia = Value(await host.Send(new RegisterJugadorCommand(scenario.RecursosHumanos, 4, "Elena", "Mejía", 9)));

        var day = new DateTime(2026, 3, 1, 15, 0, 0, DateTimeKind.Utc).AddDays(dayOffset);
        scenario.PartidoTecnologiaFinanzas = await ScheduleAsync(host, scenario, scenario.Tecnologia, scenario.Finanzas, day);
        var operacionesRrhh = await ScheduleAsync(host, scenario, scenario.Operaciones, scenario.RecursosHumanos, day.AddDays(1));
        var tecnologiaOperaciones = await ScheduleAsync(host, scenario, scenario.Tecnologia, scenario.Operaciones, day.AddDays(2));
        await ScheduleAsync(host, scenario, scenario.Finanzas, scenario.RecursosHumanos, day.AddDays(20));
        await ScheduleAsync(host, scenario, scenario.Tecnologia, scenario.RecursosHumanos, day.AddDays(21));
        await ScheduleAsync(host, scenario, scenario.Finanzas, scenario.Operaciones, day.AddDays(22));

        Ok(await host.Send(new RegisterPartidoResultCommand(scenario.PartidoTecnologiaFinanzas, 2, 1,
            [Gol(scenario.Lopez, scenario.Tecnologia, 40), Gol(scenario.Lopez, scenario.Tecnologia, 10), Gol(perez, scenario.Finanzas, 70)])));
        Ok(await host.Send(new RegisterPartidoResultCommand(operacionesRrhh, 1, 1,
            [Gol(scenario.Ramos, scenario.Operaciones, 20), Gol(mejia, scenario.RecursosHumanos, 80)])));
        Ok(await host.Send(new RegisterPartidoResultCommand(tecnologiaOperaciones, 0, 3,
            [Gol(scenario.Ramos, scenario.Operaciones, 5), Gol(scenario.Ramos, scenario.Operaciones, 55), Gol(cruz, scenario.Operaciones, 88)])));

        return scenario;
    }

    private static async Task<Guid> CreateEquipoAsync(CqrsHost host, string nombre, string abreviatura) =>
        Value(await host.Send(new CreateEquipoCommand(nombre, abreviatura, "Pruebas")));

    private static async Task<Guid> ScheduleAsync(CqrsHost host, TorneoScenario scenario, Guid local, Guid visitante, DateTime fecha) =>
        Value(await host.Send(new SchedulePartidoCommand(scenario.TorneoId, local, visitante, fecha, "Cancha 1")));

    private static DetalleGol Gol(Guid jugadorId, Guid equipoId, int minuto) => new(jugadorId, equipoId, minuto);

    private static T Value<T>(Result<T> result)
    {
        Assert.True(result.IsSuccess, result.ErrorMessage);
        return result.Value;
    }

    private static void Ok(Result result) => Assert.True(result.IsSuccess, result.ErrorMessage);
}
