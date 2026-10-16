namespace MundialitoCorporativo.Domain.Torneos;

/// <summary>
/// Inscripción de un equipo en un torneo (tabla TorneoEquipos). Pertenece al agregado <see cref="Torneo"/>.
/// </summary>
public sealed class TorneoEquipo
{
    internal TorneoEquipo(Guid torneoId, Guid equipoId, DateTime fechaInscripcion)
    {
        TorneoId = torneoId;
        EquipoId = equipoId;
        FechaInscripcion = fechaInscripcion;
    }

    private TorneoEquipo()
    {
    }

    public Guid TorneoId { get; private init; }

    public Guid EquipoId { get; private init; }

    public DateTime FechaInscripcion { get; private init; }
}
