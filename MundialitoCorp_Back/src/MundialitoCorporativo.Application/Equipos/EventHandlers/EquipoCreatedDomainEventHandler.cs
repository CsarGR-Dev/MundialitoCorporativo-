using Microsoft.Extensions.Logging;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Domain.Equipos.Events;

namespace MundialitoCorporativo.Application.Equipos.EventHandlers;

/// <summary>
/// Evento obligatorio del requerimiento (ítem 7): deja constancia en el log de la creación del equipo.
/// </summary>
internal sealed partial class EquipoCreatedDomainEventHandler(ILogger<EquipoCreatedDomainEventHandler> logger)
    : IDomainEventHandler<EquipoCreatedDomainEvent>
{
    public Task Handle(EquipoCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogEquipoCreated(logger, domainEvent.EquipoId, domainEvent.Nombre, domainEvent.Abreviatura);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Equipo creado: {EquipoId} {NombreEquipo} ({Abreviatura})")]
    private static partial void LogEquipoCreated(ILogger logger, Guid equipoId, string nombreEquipo, string abreviatura);
}
