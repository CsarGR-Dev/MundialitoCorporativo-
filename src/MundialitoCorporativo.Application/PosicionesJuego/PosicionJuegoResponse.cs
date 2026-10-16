namespace MundialitoCorporativo.Application.PosicionesJuego;

public sealed record PosicionJuegoResponse
{
    public byte Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string Abreviatura { get; init; } = string.Empty;

    public byte Orden { get; init; }

    public bool Activo { get; init; }
}
