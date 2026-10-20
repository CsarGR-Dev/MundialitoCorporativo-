using Microsoft.Extensions.Logging;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Domain.Partidos.Events;

namespace MundialitoCorporativo.Application.Partidos.EventHandlers;

/// <summary>
/// Evento obligatorio del requerimiento (ítem 7): deja constancia en el log del resultado registrado.
/// La tabla de posiciones no se actualiza aquí: se calcula al consultarla a partir de los partidos jugados.
/// </summary>
internal sealed partial class PartidoResultRegisteredDomainEventHandler(ILogger<PartidoResultRegisteredDomainEventHandler> logger)
    : IDomainEventHandler<PartidoResultRegisteredDomainEvent>
{
    public Task Handle(PartidoResultRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogResultadoRegistrado(
            logger,
            domainEvent.PartidoId,
            domainEvent.TorneoId,
            domainEvent.EquipoLocalId,
            domainEvent.GolesLocal,
            domainEvent.GolesVisitante,
            domainEvent.EquipoVisitanteId);

        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Resultado registrado en el partido {PartidoId} del torneo {TorneoId}: {EquipoLocalId} {GolesLocal} - {GolesVisitante} {EquipoVisitanteId}")]
    private static partial void LogResultadoRegistrado(
        ILogger logger,
        Guid partidoId,
        Guid torneoId,
        Guid equipoLocalId,
        int golesLocal,
        int golesVisitante,
        Guid equipoVisitanteId);
}
