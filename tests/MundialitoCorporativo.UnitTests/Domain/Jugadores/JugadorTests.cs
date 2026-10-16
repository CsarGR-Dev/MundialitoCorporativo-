using MundialitoCorporativo.Domain.Jugadores;
using MundialitoCorporativo.Domain.Jugadores.Events;
using MundialitoCorporativo.Domain.PosicionesJuego;

namespace MundialitoCorporativo.UnitTests.Domain.Jugadores;

public class JugadorTests
{
    private static readonly Guid EquipoId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldRegisterJugadorInEquipo()
    {
        var result = Jugador.Create(EquipoId, TestData.Delantero, "Ana", "López", 9);

        Assert.True(result.IsSuccess);
        Assert.Equal(EquipoId, result.Value.EquipoId);
        Assert.Equal(TestData.Delantero.Id, result.Value.PosicionJuegoId);
        Assert.True(result.Value.Activo);
        Assert.IsType<JugadorRegisteredDomainEvent>(Assert.Single(result.Value.DomainEvents));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Create_WithNumeroCamisetaOutOfRange_ShouldFail(int numeroCamiseta)
    {
        var result = Jugador.Create(EquipoId, TestData.Delantero, "Ana", "López", numeroCamiseta);

        Assert.Equal(JugadorErrors.InvalidNumeroCamiseta, result.Error);
    }

    [Fact]
    public void Create_WithInactivePosicionJuego_ShouldFail()
    {
        var result = Jugador.Create(EquipoId, TestData.PosicionInactiva, "Ana", "López", 9);

        Assert.Equal(PosicionJuegoErrors.Inactive(TestData.PosicionInactiva.Id), result.Error);
    }

    [Fact]
    public void Create_WithShortApellidos_ShouldFail()
    {
        var result = Jugador.Create(EquipoId, TestData.Delantero, "Ana", "L", 9);

        Assert.Equal(JugadorErrors.InvalidApellidos, result.Error);
    }

    [Fact]
    public void Update_ShouldKeepEquipo()
    {
        var jugador = TestData.NewJugador(EquipoId, 9);

        var result = jugador.Update(TestData.Delantero, "Ana María", "López", 10, activo: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(EquipoId, jugador.EquipoId);
        Assert.Equal(10, jugador.NumeroCamiseta);
        Assert.False(jugador.Activo);
    }
}
