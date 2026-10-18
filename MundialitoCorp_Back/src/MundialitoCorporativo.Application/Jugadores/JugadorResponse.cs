namespace MundialitoCorporativo.Application.Jugadores;

public sealed record JugadorResponse
{
    public Guid Id { get; init; }

    public Guid EquipoId { get; init; }

    public string Equipo { get; init; } = string.Empty;

    public byte PosicionJuegoId { get; init; }

    public string PosicionJuego { get; init; } = string.Empty;

    public string Nombres { get; init; } = string.Empty;

    public string Apellidos { get; init; } = string.Empty;

    public byte NumeroCamiseta { get; init; }

    public bool Activo { get; init; }

    public int Goles { get; init; }
}
