using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.UnitTests.Domain.Torneos;

public class TorneoTests
{
    [Fact]
    public void Create_ShouldStartAsPlanificado()
    {
        var result = Torneo.Create("Mundialito Corporativo 2026", "Edición anual", new DateOnly(2026, 10, 1), null);

        Assert.True(result.IsSuccess);
        Assert.Equal(EstadoTorneo.Planificado, result.Value.Estado);
        Assert.Empty(result.Value.Equipos);
    }

    [Fact]
    public void Create_WithFechaFinBeforeFechaInicio_ShouldFail()
    {
        var result = Torneo.Create("Mundialito", null, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 1));

        Assert.Equal(TorneoErrors.InvalidDateRange, result.Error);
    }

    [Fact]
    public void RegisterEquipo_Twice_ShouldReturnConflict()
    {
        var torneo = TestData.NewTorneo();
        var equipoId = Guid.NewGuid();

        Assert.True(torneo.RegisterEquipo(equipoId, TestData.UtcNow).IsSuccess);
        var second = torneo.RegisterEquipo(equipoId, TestData.UtcNow);

        Assert.Equal(TorneoErrors.EquipoAlreadyRegistered, second.Error);
        Assert.Single(torneo.Equipos);
    }

    [Fact]
    public void RegisterEquipo_InFinishedTorneo_ShouldFail()
    {
        var torneo = TestData.NewTorneo();
        torneo.ChangeStatus(EstadoTorneo.Finalizado);

        var result = torneo.RegisterEquipo(Guid.NewGuid(), TestData.UtcNow);

        Assert.Equal(TorneoErrors.AlreadyFinished, result.Error);
    }

    [Fact]
    public void ChangeStatus_Forward_ShouldSucceed()
    {
        var torneo = TestData.NewTorneo();

        Assert.True(torneo.ChangeStatus(EstadoTorneo.EnCurso).IsSuccess);
        Assert.True(torneo.ChangeStatus(EstadoTorneo.Finalizado).IsSuccess);
        Assert.Equal(EstadoTorneo.Finalizado, torneo.Estado);
    }

    [Fact]
    public void ChangeStatus_Backward_ShouldFail()
    {
        var torneo = TestData.NewTorneo();
        torneo.ChangeStatus(EstadoTorneo.EnCurso);

        var result = torneo.ChangeStatus(EstadoTorneo.Planificado);

        Assert.True(result.IsFailure);
        Assert.Equal(EstadoTorneo.EnCurso, torneo.Estado);
    }

    [Fact]
    public void WithdrawEquipo_NotRegistered_ShouldFail()
    {
        var torneo = TestData.NewTorneo();

        var result = torneo.WithdrawEquipo(Guid.NewGuid());

        Assert.Equal(TorneoErrors.EquipoNotRegistered, result.Error);
    }

    [Theory]
    [InlineData(2026, 9, 30, false)]
    [InlineData(2026, 10, 1, true)]
    [InlineData(2026, 10, 31, true)]
    [InlineData(2026, 11, 1, false)]
    public void EnsureAcceptsPartidoOn_ShouldRespectTorneoDates(int year, int month, int day, bool expected)
    {
        var torneo = TestData.NewTorneo(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        var result = torneo.EnsureAcceptsPartidoOn(new DateTime(year, month, day, 15, 0, 0, DateTimeKind.Utc));

        Assert.Equal(expected, result.IsSuccess);
    }
}
