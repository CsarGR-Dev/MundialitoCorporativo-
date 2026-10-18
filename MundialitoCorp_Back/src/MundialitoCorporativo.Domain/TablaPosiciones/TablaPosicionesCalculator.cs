namespace MundialitoCorporativo.Domain.TablaPosiciones;

public sealed record EquipoTabla(Guid EquipoId, string Equipo);

public sealed record MarcadorPartido(Guid EquipoLocalId, Guid EquipoVisitanteId, int GolesLocal, int GolesVisitante);

/// <summary>
/// Fila de la tabla de posiciones; mismas columnas que la vista vw_TablaPosiciones.
/// </summary>
public sealed record FilaTablaPosiciones(
    int Posicion,
    Guid EquipoId,
    string Equipo,
    int PartidosJugados,
    int Ganados,
    int Empatados,
    int Perdidos,
    int GolesFavor,
    int GolesContra,
    int DiferenciaGol,
    int Puntos);

/// <summary>
/// Calcula la tabla de posiciones de un torneo a partir de sus partidos jugados.
/// Orden del requerimiento (ítem 8): 1. Puntos, 2. Diferencia de gol, 3. Goles a favor.
/// Desempate final por nombre para obtener un orden determinista (criterio propio, RN-10).
/// </summary>
public static class TablaPosicionesCalculator
{
    public static IReadOnlyList<FilaTablaPosiciones> Calculate(IEnumerable<EquipoTabla> equipos, IEnumerable<MarcadorPartido> partidosJugados)
    {
        var accumulators = equipos.ToDictionary(equipo => equipo.EquipoId, equipo => new Accumulator(equipo));

        foreach (var partido in partidosJugados)
        {
            if (accumulators.TryGetValue(partido.EquipoLocalId, out var home))
            {
                home.Add(partido.GolesLocal, partido.GolesVisitante);
            }

            if (accumulators.TryGetValue(partido.EquipoVisitanteId, out var away))
            {
                away.Add(partido.GolesVisitante, partido.GolesLocal);
            }
        }

        return accumulators.Values
            .OrderByDescending(row => row.Puntos)
            .ThenByDescending(row => row.DiferenciaGol)
            .ThenByDescending(row => row.GolesFavor)
            .ThenBy(row => row.Equipo.Equipo, StringComparer.CurrentCultureIgnoreCase)
            .Select((row, index) => row.ToFila(index + 1))
            .ToList();
    }

    private sealed class Accumulator(EquipoTabla equipo)
    {
        public EquipoTabla Equipo { get; } = equipo;

        public int PartidosJugados { get; private set; }

        public int Ganados { get; private set; }

        public int Empatados { get; private set; }

        public int Perdidos { get; private set; }

        public int GolesFavor { get; private set; }

        public int GolesContra { get; private set; }

        public int DiferenciaGol => GolesFavor - GolesContra;

        public int Puntos =>
            (Ganados * ScoringRules.PointsPerWin)
            + (Empatados * ScoringRules.PointsPerDraw)
            + (Perdidos * ScoringRules.PointsPerLoss);

        public void Add(int golesFavor, int golesContra)
        {
            PartidosJugados++;
            GolesFavor += golesFavor;
            GolesContra += golesContra;

            if (golesFavor > golesContra)
            {
                Ganados++;
            }
            else if (golesFavor == golesContra)
            {
                Empatados++;
            }
            else
            {
                Perdidos++;
            }
        }

        public FilaTablaPosiciones ToFila(int posicion) =>
            new(posicion, Equipo.EquipoId, Equipo.Equipo, PartidosJugados, Ganados, Empatados, Perdidos, GolesFavor, GolesContra, DiferenciaGol, Puntos);
    }
}
