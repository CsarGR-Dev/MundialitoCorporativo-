using MundialitoCorporativo.Domain.Jugadores;
using MundialitoCorporativo.Domain.Partidos;
using MundialitoCorporativo.Domain.Partidos.Events;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.UnitTests.Domain.Partidos;

public class PartidoTests
{
    private static readonly DateTime FechaProgramada = new(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

    private readonly Torneo _torneo = TestData.NewTorneo();
    private readonly Guid _equipoLocalId = Guid.NewGuid();
    private readonly Guid _equipoVisitanteId = Guid.NewGuid();
    private readonly Jugador _jugadorLocal;
    private readonly Jugador _jugadorVisitante;

    public PartidoTests()
    {
        _torneo.RegisterEquipo(_equipoLocalId, TestData.UtcNow);
        _torneo.RegisterEquipo(_equipoVisitanteId, TestData.UtcNow);
        _jugadorLocal = TestData.NewJugador(_equipoLocalId, 9);
        _jugadorVisitante = TestData.NewJugador(_equipoVisitanteId, 9);
    }

    [Fact]
    public void Schedule_WithRegisteredEquipos_ShouldCreateProgramadoPartido()
    {
        var result = Partido.Schedule(_torneo, _equipoLocalId, _equipoVisitanteId, FechaProgramada.AddMilliseconds(450), "Cancha 1");

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoPartido.Programado, result.Value.Estado);
        Assert.Equal(FechaProgramada, result.Value.FechaProgramada);
        Assert.IsType<PartidoScheduledDomainEvent>(Assert.Single(result.Value.DomainEvents));
    }

    [Fact]
    public void Schedule_WithSameEquipo_ShouldFail()
    {
        var result = Partido.Schedule(_torneo, _equipoLocalId, _equipoLocalId, FechaProgramada, null);

        Assert.Equal(PartidoErrors.SameEquipo, result.Error);
    }

    [Fact]
    public void Schedule_WithEquipoNotRegistered_ShouldFail()
    {
        var result = Partido.Schedule(_torneo, _equipoLocalId, Guid.NewGuid(), FechaProgramada, null);

        Assert.Equal(TorneoErrors.EquipoNotRegistered, result.Error);
    }

    [Fact]
    public void RegisterResult_WithConsistentGoles_ShouldMarkJugadoAndRaiseEvent()
    {
        var partido = SchedulePartido();

        var result = partido.RegisterResult(
            _torneo,
            golesLocal: 2,
            golesVisitante: 1,
            [Gol(_jugadorLocal, 10), Gol(_jugadorLocal, 40), Gol(_jugadorVisitante, 70)],
            [_jugadorLocal, _jugadorVisitante],
            TestData.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoPartido.Jugado, partido.Estado);
        Assert.Equal(2, partido.GolesLocal);
        Assert.Equal(1, partido.GolesVisitante);
        Assert.Equal(3, partido.Goles.Count);
        Assert.Contains(partido.DomainEvents, domainEvent => domainEvent is PartidoResultRegisteredDomainEvent);
    }

    [Fact]
    public void RegisterResult_Twice_ShouldReturnConflict()
    {
        var partido = SchedulePartido();
        partido.RegisterResult(_torneo, 0, 0, [], [], TestData.UtcNow);

        var second = partido.RegisterResult(_torneo, 1, 0, [Gol(_jugadorLocal, 5)], [_jugadorLocal], TestData.UtcNow);

        Assert.Equal(PartidoErrors.AlreadyPlayed, second.Error);
        Assert.Equal(0, partido.GolesLocal);
    }

    [Fact]
    public void RegisterResult_WhenGolesDoNotMatchScore_ShouldFail()
    {
        var partido = SchedulePartido();

        var result = partido.RegisterResult(_torneo, 2, 0, [Gol(_jugadorLocal, 10)], [_jugadorLocal], TestData.UtcNow);

        Assert.Equal(PartidoErrors.GolesMismatch, result.Error);
        Assert.Equal(EstadoPartido.Programado, partido.Estado);
        Assert.Empty(partido.Goles);
    }

    [Fact]
    public void RegisterResult_WhenScorerBelongsToOtherEquipo_ShouldFail()
    {
        var partido = SchedulePartido();
        var golCreditedToWrongEquipo = new DetalleGol(_jugadorVisitante.Id, _equipoLocalId, 10);

        var result = partido.RegisterResult(_torneo, 1, 0, [golCreditedToWrongEquipo], [_jugadorVisitante], TestData.UtcNow);

        Assert.Equal(PartidoErrors.ScorerNotInEquipo, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(131)]
    public void RegisterResult_WithMinutoOutOfRange_ShouldFail(int minuto)
    {
        var partido = SchedulePartido();

        var result = partido.RegisterResult(_torneo, 1, 0, [Gol(_jugadorLocal, minuto)], [_jugadorLocal], TestData.UtcNow);

        Assert.Equal(PartidoErrors.InvalidMinuto, result.Error);
    }

    [Fact]
    public void RegisterResult_OnCancelledPartido_ShouldFail()
    {
        var partido = SchedulePartido();
        partido.Cancel();

        var result = partido.RegisterResult(_torneo, 0, 0, [], [], TestData.UtcNow);

        Assert.Equal(PartidoErrors.IsCancelled, result.Error);
    }

    [Fact]
    public void Delete_PlayedPartido_ShouldFail()
    {
        var partido = SchedulePartido();
        partido.RegisterResult(_torneo, 0, 0, [], [], TestData.UtcNow);

        var result = partido.Delete(TestData.UtcNow);

        Assert.Equal(PartidoErrors.PlayedCannotBeDeleted, result.Error);
        Assert.False(partido.Eliminado);
    }

    [Fact]
    public void Update_PlayedPartido_ShouldFail()
    {
        var partido = SchedulePartido();
        partido.RegisterResult(_torneo, 0, 0, [], [], TestData.UtcNow);

        var result = partido.Update(_torneo, _equipoLocalId, _equipoVisitanteId, FechaProgramada.AddDays(1), null);

        Assert.Equal(PartidoErrors.NotScheduled, result.Error);
    }

    private Partido SchedulePartido() =>
        Partido.Schedule(_torneo, _equipoLocalId, _equipoVisitanteId, FechaProgramada, null).Value;

    private static DetalleGol Gol(Jugador jugador, int minuto) => new(jugador.Id, jugador.EquipoId, minuto);
}
