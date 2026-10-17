using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Abstractions.Messaging;

/// <summary>
/// Reacción a un evento de dominio. Se ejecuta en proceso, después de que la transacción se confirmó.
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Entrega cada evento de dominio a todos sus handlers registrados. Lo usa el Unit of Work tras el commit.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
