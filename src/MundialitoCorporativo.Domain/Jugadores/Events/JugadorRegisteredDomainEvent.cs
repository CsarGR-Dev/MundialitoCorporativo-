using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Domain.Jugadores.Events;

/// <summary>
/// Se registra un integrante en un equipo.
/// </summary>
public sealed record JugadorRegisteredDomainEvent(Guid JugadorId, Guid EquipoId, int NumeroCamiseta) : DomainEvent;
