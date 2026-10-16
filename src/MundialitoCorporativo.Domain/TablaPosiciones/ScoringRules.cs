namespace MundialitoCorporativo.Domain.TablaPosiciones;

/// <summary>
/// Puntos por resultado: victoria 3, empate 1, derrota 0 (RN-09).
/// </summary>
public static class ScoringRules
{
    public const int PointsPerWin = 3;
    public const int PointsPerDraw = 1;
    public const int PointsPerLoss = 0;

    public static int PointsFor(int golesFavor, int golesContra) =>
        golesFavor > golesContra ? PointsPerWin
        : golesFavor == golesContra ? PointsPerDraw
        : PointsPerLoss;
}
