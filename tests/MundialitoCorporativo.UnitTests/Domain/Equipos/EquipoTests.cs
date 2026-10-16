using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Equipos.Events;

namespace MundialitoCorporativo.UnitTests.Domain.Equipos;

public class EquipoTests
{
    [Fact]
    public void Create_WithValidData_ShouldRaiseEquipoCreatedEvent()
    {
        var result = Equipo.Create("  Tecnología FC ", "tec", "Tecnología");

        Assert.True(result.IsSuccess);
        var equipo = result.Value;
        Assert.Equal("Tecnología FC", equipo.Nombre);
        Assert.Equal("TEC", equipo.Abreviatura);
        Assert.NotEqual(Guid.Empty, equipo.Id);

        var domainEvent = Assert.IsType<EquipoCreatedDomainEvent>(Assert.Single(equipo.DomainEvents));
        Assert.Equal(equipo.Id, domainEvent.EquipoId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("   ")]
    public void Create_WithInvalidNombre_ShouldFail(string nombre)
    {
        var result = Equipo.Create(nombre, "TEC", null);

        Assert.True(result.IsFailure);
        Assert.Equal(EquipoErrors.InvalidNombre, result.Error);
    }

    [Theory]
    [InlineData("T")]
    [InlineData("MUYLARGA")]
    public void Create_WithInvalidAbreviatura_ShouldFail(string abreviatura)
    {
        var result = Equipo.Create("Tecnología FC", abreviatura, null);

        Assert.Equal(EquipoErrors.InvalidAbreviatura, result.Error);
    }

    [Fact]
    public void Update_ShouldReplaceAllFields()
    {
        var equipo = TestData.NewEquipo("Tecnología FC", "TEC");

        var result = equipo.Update("Tecnología United", "tu", null);

        Assert.True(result.IsSuccess);
        Assert.Equal("Tecnología United", equipo.Nombre);
        Assert.Equal("TU", equipo.Abreviatura);
        Assert.Null(equipo.Departamento);
    }

    [Fact]
    public void Delete_Twice_ShouldKeepSameState()
    {
        var equipo = TestData.NewEquipo("Tecnología FC", "TEC");

        Assert.True(equipo.Delete(TestData.UtcNow).IsSuccess);
        Assert.True(equipo.Delete(TestData.UtcNow.AddHours(1)).IsSuccess);

        Assert.True(equipo.Eliminado);
        Assert.Equal(TestData.UtcNow, equipo.FechaEliminacion);
    }
}
