using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.UnitTests.Infrastructure;

/// <summary>
/// Despachador de eventos de dominio para pruebas: guarda lo despachado y puede simular un handler que falla.
/// </summary>
internal sealed class RecordingDomainEventDispatcher : IDomainEventDispatcher
{
    public List<IDomainEvent> Dispatched { get; } = [];

    public Exception? FailWith { get; set; }

    /// <summary>Se ejecuta en el momento del despacho, para verificar el estado en ese instante.</summary>
    public Action<IDomainEvent>? OnDispatch { get; set; }

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        OnDispatch?.Invoke(domainEvent);

        if (FailWith is not null)
        {
            throw FailWith;
        }

        Dispatched.Add(domainEvent);
        return Task.CompletedTask;
    }
}
