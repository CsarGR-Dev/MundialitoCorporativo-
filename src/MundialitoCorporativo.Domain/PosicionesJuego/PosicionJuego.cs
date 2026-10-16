namespace MundialitoCorporativo.Domain.PosicionesJuego;

/// <summary>
/// Catálogo de posiciones de juego (tabla PosicionesJuego). Datos de referencia de solo lectura.
/// </summary>
public sealed class PosicionJuego
{
    public PosicionJuego(byte id, string nombre, string abreviatura, byte orden, bool activo = true)
    {
        Id = id;
        Nombre = nombre;
        Abreviatura = abreviatura;
        Orden = orden;
        Activo = activo;
    }

    private PosicionJuego()
    {
    }

    public byte Id { get; private init; }

    public string Nombre { get; private init; } = string.Empty;

    public string Abreviatura { get; private init; } = string.Empty;

    public byte Orden { get; private init; }

    public bool Activo { get; private init; }
}
