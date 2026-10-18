using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Domain.Equipos.Events;

/// <summary>
/// Evento obligatorio del requerimiento (ítem 7): se crea un equipo.
/// </summary>
public sealed record EquipoCreatedDomainEvent(Guid EquipoId, string Nombre, string Abreviatura) : DomainEvent;
