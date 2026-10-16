namespace MundialitoCorporativo.Application.Torneos;

public sealed record TorneoResponse
{
    public Guid Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string? Descripcion { get; init; }

    public DateOnly FechaInicio { get; init; }

    public DateOnly? FechaFin { get; init; }

    public string Estado { get; init; } = string.Empty;

    public int CantidadEquipos { get; init; }

    public int CantidadPartidos { get; init; }
}

public sealed record TorneoEquipoResponse
{
    public Guid EquipoId { get; init; }

    public string Equipo { get; init; } = string.Empty;

    public string Abreviatura { get; init; } = string.Empty;

    public string? Departamento { get; init; }

    public DateTimeOffset FechaInscripcion { get; init; }
}

/// <summary>
/// Fila de la tabla de posiciones (mismas columnas que la vista vw_TablaPosiciones).
/// </summary>
public sealed record FilaTablaPosicionesResponse
{
    public int Posicion { get; init; }

    public Guid EquipoId { get; init; }

    public string Equipo { get; init; } = string.Empty;

    public string Abreviatura { get; init; } = string.Empty;

    public int PartidosJugados { get; init; }

    public int Ganados { get; init; }

    public int Empatados { get; init; }

    public int Perdidos { get; init; }

    public int GolesFavor { get; init; }

    public int GolesContra { get; init; }

    public int DiferenciaGol { get; init; }

    public int Puntos { get; init; }
}

public sealed record GoleadorResponse
{
    public int Posicion { get; init; }

    public Guid JugadorId { get; init; }

    public string Jugador { get; init; } = string.Empty;

    public byte NumeroCamiseta { get; init; }

    public string PosicionJuego { get; init; } = string.Empty;

    public Guid EquipoId { get; init; }

    public string Equipo { get; init; } = string.Empty;

    public int Goles { get; init; }

    public int PartidosConGol { get; init; }
}
