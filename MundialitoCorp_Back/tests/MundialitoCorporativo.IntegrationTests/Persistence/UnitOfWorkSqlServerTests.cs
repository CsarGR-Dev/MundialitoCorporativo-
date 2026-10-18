using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Jugadores;
using MundialitoCorporativo.Domain.Partidos;
using MundialitoCorporativo.Domain.Torneos;
using MundialitoCorporativo.Infrastructure.Persistence;
using MundialitoCorporativo.Infrastructure.Persistence.Repositories;

namespace MundialitoCorporativo.IntegrationTests.Persistence;

/// <summary>
/// Valida el lado de escritura (EF Core + UnitOfWork) contra SQL Server, sobre el esquema creado con Db/001.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class UnitOfWorkSqlServerTests(SqlServerDatabaseFixture fixture)
{
    private static readonly DateTime UtcNow = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaProgramada = new(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

    [SkippableFact]
    public async Task CommitAsync_FullTorneoFlow_ShouldRoundTripAgainstScriptSchema()
    {
        SkipIfUnavailable();
        var (torneo, local, visitante) = await CreateTorneoWithEquiposAsync();

        Guid partidoId;
        await using (var context = fixture.CreateContext())
        {
            var posicion = await new PosicionJuegoRepository(context).GetByIdAsync(4);
            Assert.NotNull(posicion);
            Assert.Equal("Delantero", posicion.Nombre);

            var goleador = Jugador.Create(local.Id, posicion, "Ana", "López", 9).Value;
            new JugadorRepository(context).Add(goleador);

            var torneoGuardado = (await new TorneoRepository(context).GetByIdAsync(torneo.Id))!;
            var partido = Partido.Schedule(torneoGuardado, local.Id, visitante.Id, FechaProgramada, "Cancha 1").Value;
            partido.RegisterResult(torneoGuardado, 1, 0, [new DetalleGol(goleador.Id, local.Id, 30)], [goleador], UtcNow);
            new PartidoRepository(context).Add(partido);
            partidoId = partido.Id;

            var result = await CreateUnitOfWork(context).CommitAsync();
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        await using var verification = fixture.CreateContext();
        var stored = await new PartidoRepository(verification).GetByIdAsync(partidoId);

        Assert.NotNull(stored);
        Assert.Equal(EstadoPartido.Jugado, stored.Estado);
        Assert.Equal(1, stored.GolesLocal);
        Assert.Equal(0, stored.GolesVisitante);
        Assert.Equal(FechaProgramada, stored.FechaProgramada);
        Assert.Equal(DateTimeKind.Utc, stored.FechaProgramada.Kind);
        Assert.Equal(30, Assert.Single(stored.Goles).Minuto);

        var estadoEnBase = await verification.Database
            .SqlQuery<string>($"SELECT Estado AS Value FROM dbo.Partidos WHERE Id = {partidoId}")
            .SingleAsync();
        Assert.Equal("Jugado", estadoEnBase);

        var eventos = await verification.EventosDominio.Where(evento => evento.AgregadoId == partidoId).ToListAsync();
        Assert.Contains(eventos, evento => evento.TipoEvento == "PartidoResultRegisteredDomainEvent");
    }

    [SkippableFact]
    public async Task CommitAsync_WhenUniqueIndexIsViolated_ShouldReturnConflictAndRollBackEverything()
    {
        SkipIfUnavailable();
        var nombre = $"Equipo {Guid.NewGuid():N}"[..20];

        await using (var context = fixture.CreateContext())
        {
            new EquipoRepository(context).Add(Equipo.Create(nombre, "AAA", null).Value);
            Assert.True((await CreateUnitOfWork(context).CommitAsync()).IsSuccess);
        }

        var otroEquipoValido = Equipo.Create($"Otro {Guid.NewGuid():N}"[..20], "BBB", null).Value;
        await using (var context = fixture.CreateContext())
        {
            var repository = new EquipoRepository(context);
            repository.Add(otroEquipoValido);
            repository.Add(Equipo.Create(nombre.ToUpperInvariant(), "CCC", null).Value);

            var result = await CreateUnitOfWork(context).CommitAsync();

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Conflict, result.Error.Type);
            Assert.Equal("Database.ConstraintViolation", result.ErrorCode);
        }

        await using var verification = fixture.CreateContext();
        Assert.False(await verification.Equipos.AnyAsync(equipo => equipo.Id == otroEquipoValido.Id));
        Assert.False(await verification.EventosDominio.AnyAsync(evento => evento.AgregadoId == otroEquipoValido.Id));
    }

    [SkippableFact]
    public async Task CommitAsync_WhenTwoUsersRegisterSameResult_ShouldReturnConcurrencyConflict()
    {
        SkipIfUnavailable();
        var (torneo, local, visitante) = await CreateTorneoWithEquiposAsync();

        Guid partidoId;
        await using (var context = fixture.CreateContext())
        {
            var torneoGuardado = (await new TorneoRepository(context).GetByIdAsync(torneo.Id))!;
            var partido = Partido.Schedule(torneoGuardado, local.Id, visitante.Id, FechaProgramada.AddDays(1), null).Value;
            new PartidoRepository(context).Add(partido);
            partidoId = partido.Id;
            await CreateUnitOfWork(context).CommitAsync();
        }

        await using var firstUser = fixture.CreateContext();
        await using var secondUser = fixture.CreateContext();
        var torneoFirst = (await new TorneoRepository(firstUser).GetByIdAsync(torneo.Id))!;
        var torneoSecond = (await new TorneoRepository(secondUser).GetByIdAsync(torneo.Id))!;
        var partidoFirst = (await new PartidoRepository(firstUser).GetByIdAsync(partidoId))!;
        var partidoSecond = (await new PartidoRepository(secondUser).GetByIdAsync(partidoId))!;

        partidoFirst.RegisterResult(torneoFirst, 0, 0, [], [], UtcNow);
        partidoSecond.RegisterResult(torneoSecond, 0, 0, [], [], UtcNow);

        var first = await CreateUnitOfWork(firstUser).CommitAsync();
        var second = await CreateUnitOfWork(secondUser).CommitAsync();

        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.True(second.IsFailure);
        Assert.Equal("Database.ConcurrencyConflict", second.ErrorCode);
    }

    [SkippableFact]
    public async Task CommitAsync_SoftDeletedEquipo_ShouldBeHiddenAndReleaseNombre()
    {
        SkipIfUnavailable();
        var nombre = $"Borrado {Guid.NewGuid():N}"[..20];
        var equipo = Equipo.Create(nombre, "DDD", null).Value;

        await using (var context = fixture.CreateContext())
        {
            new EquipoRepository(context).Add(equipo);
            await CreateUnitOfWork(context).CommitAsync();
        }

        await using (var context = fixture.CreateContext())
        {
            var repository = new EquipoRepository(context);
            (await repository.GetByIdAsync(equipo.Id))!.Delete(UtcNow);
            Assert.True((await CreateUnitOfWork(context).CommitAsync()).IsSuccess);
        }

        await using var verification = fixture.CreateContext();
        var verificationRepository = new EquipoRepository(verification);
        Assert.Null(await verificationRepository.GetByIdAsync(equipo.Id));
        Assert.NotNull(await verificationRepository.GetByIdIncludingDeletedAsync(equipo.Id));
        Assert.False(await verificationRepository.ExistsByNombreAsync(nombre));
    }

    private async Task<(Torneo Torneo, Equipo Local, Equipo Visitante)> CreateTorneoWithEquiposAsync()
    {
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var local = Equipo.Create($"Local {sufijo}", AbreviaturaUnica(), null).Value;
        var visitante = Equipo.Create($"Visitante {sufijo}", AbreviaturaUnica(), null).Value;
        var torneo = Torneo.Create($"Torneo {sufijo}", null, new DateOnly(2026, 10, 1), null).Value;
        torneo.RegisterEquipo(local.Id, UtcNow);
        torneo.RegisterEquipo(visitante.Id, UtcNow);

        await using var context = fixture.CreateContext();
        new EquipoRepository(context).Add(local);
        new EquipoRepository(context).Add(visitante);
        new TorneoRepository(context).Add(torneo);

        var result = await CreateUnitOfWork(context).CommitAsync();
        Assert.True(result.IsSuccess, result.ErrorMessage);

        return (torneo, local, visitante);
    }

    private static string AbreviaturaUnica() => UniqueTestData.Abreviatura('U');

    private void SkipIfUnavailable() => Skip.IfNot(fixture.IsAvailable, fixture.UnavailableReason);

    private static UnitOfWork CreateUnitOfWork(MundialitoDbContext context) =>
        new(context, new FixedDateTimeProvider(UtcNow), new NoHandlersDomainEventDispatcher(), NullLogger<UnitOfWork>.Instance);

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class NoHandlersDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
