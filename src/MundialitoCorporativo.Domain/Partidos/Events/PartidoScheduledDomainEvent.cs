using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Domain.Partidos.Events;

/// <summary>
/// Se programa un partido en el calendario del torneo.
/// </summary>
public sealed record PartidoScheduledDomainEvent(
    Guid PartidoId,
    Guid TorneoId,
    Guid EquipoLocalId,
    Guid EquipoVisitanteId,
    DateTime FechaProgramada) : DomainEvent;
