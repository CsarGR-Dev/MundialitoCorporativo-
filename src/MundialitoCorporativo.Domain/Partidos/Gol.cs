using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Domain.Partidos;

/// <summary>
/// Gol anotado en un partido (tabla Goles). Pertenece al agregado <see cref="Partido"/>; base de la tabla de goleadores.
/// </summary>
public sealed class Gol : Entity
{
    internal Gol(Guid id, Guid partidoId, Guid jugadorId, Guid equipoId, byte minuto)
        : base(id)
    {
        PartidoId = partidoId;
        JugadorId = jugadorId;
        EquipoId = equipoId;
        Minuto = minuto;
    }

    private Gol()
    {
    }

    public Guid PartidoId { get; private init; }

    public Guid JugadorId { get; private init; }

    public Guid EquipoId { get; private init; }

    public byte Minuto { get; private init; }
}

/// <summary>
/// Datos de un gol al registrar el resultado de un partido.
/// </summary>
public sealed record DetalleGol(Guid JugadorId, Guid EquipoId, int Minuto);
