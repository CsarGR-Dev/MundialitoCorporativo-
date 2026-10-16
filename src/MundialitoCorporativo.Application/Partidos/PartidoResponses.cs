namespace MundialitoCorporativo.Application.Partidos;

public record PartidoResponse
{
    public Guid Id { get; init; }

    public Guid TorneoId { get; init; }

    public string Torneo { get; init; } = string.Empty;

    public Guid EquipoLocalId { get; init; }

    public string EquipoLocal { get; init; } = string.Empty;

    public Guid EquipoVisitanteId { get; init; }

    public string EquipoVisitante { get; init; } = string.Empty;

    public DateTimeOffset FechaProgramada { get; init; }

    public string? Sede { get; init; }

    public string Estado { get; init; } = string.Empty;

    public int? GolesLocal { get; init; }

    public int? GolesVisitante { get; init; }

    public DateTimeOffset? FechaJugado { get; init; }
}

public sealed record PartidoDetalleResponse : PartidoResponse
{
    public IReadOnlyList<GolResponse> Goles { get; init; } = [];
}

public sealed record GolResponse
{
    public Guid Id { get; init; }

    public Guid JugadorId { get; init; }

    public string Jugador { get; init; } = string.Empty;

    public Guid EquipoId { get; init; }

    public string Equipo { get; init; } = string.Empty;

    public byte Minuto { get; init; }
}
