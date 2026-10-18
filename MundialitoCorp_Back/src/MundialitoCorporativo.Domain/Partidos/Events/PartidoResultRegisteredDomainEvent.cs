using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Domain.Partidos.Events;

/// <summary>
/// Evento obligatorio del requerimiento (ítem 7): se registra el resultado de un partido.
/// </summary>
public sealed record PartidoResultRegisteredDomainEvent(
    Guid PartidoId,
    Guid TorneoId,
    Guid EquipoLocalId,
    Guid EquipoVisitanteId,
    int GolesLocal,
    int GolesVisitante) : DomainEvent;
