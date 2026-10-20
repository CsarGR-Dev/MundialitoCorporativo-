using MediatR;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Abstractions.Messaging;

/// <summary>
/// Notificación de MediatR que transporta un evento de dominio. Domain no conoce MediatR:
/// el evento se envuelve aquí, en Application, al publicarlo.
/// </summary>
public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IDomainEvent;

/// <summary>
/// Reacción a un evento de dominio. Se ejecuta en proceso, después de que la transacción se confirmó.
/// MediatR la invoca como handler de <see cref="DomainEventNotification{TEvent}"/>.
/// </summary>
public interface IDomainEventHandler<TEvent> : INotificationHandler<DomainEventNotification<TEvent>>
    where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken);

    Task INotificationHandler<DomainEventNotification<TEvent>>.Handle(
        DomainEventNotification<TEvent> notification,
        CancellationToken cancellationToken) =>
        Handle(notification.DomainEvent, cancellationToken);
}

/// <summary>
/// Entrega cada evento de dominio a todos sus handlers registrados. Lo usa el Unit of Work tras el commit.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
