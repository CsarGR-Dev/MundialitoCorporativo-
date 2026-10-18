using MundialitoCorporativo.Domain.TablaPosiciones;

namespace MundialitoCorporativo.UnitTests.Domain.TablaPosiciones;

/// <summary>
/// Pruebas mínimas del ítem 15: tabla de posiciones, cálculo de puntos y diferencia de gol.
/// </summary>
public class TablaPosicionesCalculatorTests
{
    private static readonly EquipoTabla Tecnologia = new(Guid.NewGuid(), "Tecnología FC");
    private static readonly EquipoTabla Finanzas = new(Guid.NewGuid(), "Finanzas United");
    private static readonly EquipoTabla Operaciones = new(Guid.NewGuid(), "Operaciones City");
    private static readonly EquipoTabla RecursosHumanos = new(Guid.NewGuid(), "Recursos Humanos Athletic");

    // ---- Cálculo de puntos ----

    [Theory]
    [InlineData(2, 1, 3)]
    [InlineData(1, 1, 1)]
    [InlineData(0, 3, 0)]
    public void PointsFor_ShouldApplyWinDrawLossRule(int golesFavor, int golesContra, int expectedPoints)
    {
        Assert.Equal(expectedPoints, ScoringRules.PointsFor(golesFavor, golesContra));
    }

    [Fact]
    public void Calculate_Win_ShouldGiveThreePointsToWinnerAndZeroToLoser()
    {
        var tabla = TablaPosicionesCalculator.Calculate([Tecnologia, Finanzas], [Marcador(Tecnologia, 2, Finanzas, 1)]);

        Assert.Equal(3, Fila(tabla, Tecnologia).Puntos);
        Assert.Equal(0, Fila(tabla, Finanzas).Puntos);
    }

    [Fact]
    public void Calculate_Draw_ShouldGiveOnePointToEachEquipo()
    {
        var tabla = TablaPosicionesCalculator.Calculate([Tecnologia, Finanzas], [Marcador(Tecnologia, 1, Finanzas, 1)]);

        Assert.All(tabla, fila => Assert.Equal(1, fila.Puntos));
        Assert.All(tabla, fila => Assert.Equal(1, fila.Empatados));
    }

    // ---- Diferencia de gol ----

    [Fact]
    public void Calculate_DiferenciaGol_ShouldAccumulateAcrossPartidos()
    {
        var tabla = TablaPosicionesCalculator.Calculate(
            [Tecnologia, Finanzas, Operaciones],
            [Marcador(Tecnologia, 2, Finanzas, 1), Marcador(Tecnologia, 0, Operaciones, 3)]);

        var tecnologia = Fila(tabla, Tecnologia);
        Assert.Equal(2, tecnologia.GolesFavor);
        Assert.Equal(4, tecnologia.GolesContra);
        Assert.Equal(-2, tecnologia.DiferenciaGol);
    }

    [Fact]
    public void Calculate_EquipoWithoutPartidos_ShouldHaveZeros()
    {
        var tabla = TablaPosicionesCalculator.Calculate([Tecnologia, Finanzas, RecursosHumanos], [Marcador(Tecnologia, 1, Finanzas, 0)]);

        var fila = Fila(tabla, RecursosHumanos);
        Assert.Equal(0, fila.PartidosJugados);
        Assert.Equal(0, fila.DiferenciaGol);
        Assert.Equal(0, fila.Puntos);
    }

    // ---- Tabla de posiciones: orden 1. Puntos, 2. Diferencia de gol, 3. Goles a favor ----

    [Fact]
    public void Calculate_ShouldOrderByPuntosFirst()
    {
        var tabla = TablaPosicionesCalculator.Calculate([Finanzas, Tecnologia], [Marcador(Tecnologia, 1, Finanzas, 0)]);

        Assert.Equal(Tecnologia.EquipoId, tabla[0].EquipoId);
        Assert.Equal(1, tabla[0].Posicion);
    }

    [Fact]
    public void Calculate_WithEqualPuntos_ShouldOrderByDiferenciaGol()
    {
        // Ambos ganan un partido (3 pts); Operaciones con mejor diferencia de gol.
        var tabla = TablaPosicionesCalculator.Calculate(
            [Tecnologia, Finanzas, Operaciones, RecursosHumanos],
            [Marcador(Tecnologia, 1, Finanzas, 0), Marcador(Operaciones, 3, RecursosHumanos, 0)]);

        Assert.Equal(Operaciones.EquipoId, tabla[0].EquipoId);
        Assert.Equal(Tecnologia.EquipoId, tabla[1].EquipoId);
    }

    [Fact]
    public void Calculate_WithEqualPuntosAndDiferenciaGol_ShouldOrderByGolesFavor()
    {
        // Ambos ganan por un gol de diferencia; Operaciones anotó más goles.
        var tabla = TablaPosicionesCalculator.Calculate(
            [Tecnologia, Finanzas, Operaciones, RecursosHumanos],
            [Marcador(Tecnologia, 1, Finanzas, 0), Marcador(Operaciones, 3, RecursosHumanos, 2)]);

        Assert.Equal(Operaciones.EquipoId, tabla[0].EquipoId);
        Assert.Equal(Tecnologia.EquipoId, tabla[1].EquipoId);
    }

    [Fact]
    public void Calculate_WithCompleteTie_ShouldOrderByNombre()
    {
        var tabla = TablaPosicionesCalculator.Calculate([Tecnologia, Finanzas], [Marcador(Tecnologia, 0, Finanzas, 0)]);

        Assert.Equal(Finanzas.EquipoId, tabla[0].EquipoId);
    }

    [Fact]
    public void Calculate_WithSeedData_ShouldProduceExpectedTabla()
    {
        // Datos del seed (ítem 9): tabla esperada documentada en Analisis/09_Seed_Datos.md.
        var tabla = TablaPosicionesCalculator.Calculate(
            [Tecnologia, Finanzas, Operaciones, RecursosHumanos],
            [
                Marcador(Tecnologia, 2, Finanzas, 1),
                Marcador(Operaciones, 1, RecursosHumanos, 1),
                Marcador(Tecnologia, 0, Operaciones, 3)
            ]);

        Assert.Collection(
            tabla,
            fila => AssertFila(fila, 1, Operaciones, played: 2, puntos: 4, diferenciaGol: 3, golesFavor: 4),
            fila => AssertFila(fila, 2, Tecnologia, played: 2, puntos: 3, diferenciaGol: -2, golesFavor: 2),
            fila => AssertFila(fila, 3, RecursosHumanos, played: 1, puntos: 1, diferenciaGol: 0, golesFavor: 1),
            fila => AssertFila(fila, 4, Finanzas, played: 1, puntos: 0, diferenciaGol: -1, golesFavor: 1));
    }

    private static MarcadorPartido Marcador(EquipoTabla local, int golesLocal, EquipoTabla visitante, int golesVisitante) =>
        new(local.EquipoId, visitante.EquipoId, golesLocal, golesVisitante);

    private static FilaTablaPosiciones Fila(IReadOnlyList<FilaTablaPosiciones> tabla, EquipoTabla equipo) =>
        tabla.Single(fila => fila.EquipoId == equipo.EquipoId);

    private static void AssertFila(FilaTablaPosiciones fila, int posicion, EquipoTabla equipo, int played, int puntos, int diferenciaGol, int golesFavor)
    {
        Assert.Equal(posicion, fila.Posicion);
        Assert.Equal(equipo.EquipoId, fila.EquipoId);
        Assert.Equal(played, fila.PartidosJugados);
        Assert.Equal(puntos, fila.Puntos);
        Assert.Equal(diferenciaGol, fila.DiferenciaGol);
        Assert.Equal(golesFavor, fila.GolesFavor);
    }
}
