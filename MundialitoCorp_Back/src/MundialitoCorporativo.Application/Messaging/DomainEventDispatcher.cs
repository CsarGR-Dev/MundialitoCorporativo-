using System.Collections.Concurrent;
using System.Diagnostics;
using MediatR;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Messaging;

/// <summary>
/// Despacho en proceso de eventos de dominio (sin bus externo): envuelve el evento en una
/// <see cref="DomainEventNotification{TEvent}"/> y lo publica con MediatR, que lo entrega a sus handlers.
/// Además deja el evento marcado en la traza actual para verlo junto al request que lo originó.
/// </summary>
internal sealed class DomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type> NotificationTypes = new();

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();

        Activity.Current?.AddEvent(new ActivityEvent(
            eventType.Name,
            tags: new ActivityTagsCollection { ["mundialito.event.id"] = domainEvent.EventId.ToString() }));

        var notificationType = NotificationTypes.GetOrAdd(eventType, type => typeof(DomainEventNotification<>).MakeGenericType(type));
        var notification = Activator.CreateInstance(notificationType, domainEvent)!;

        return publisher.Publish(notification, cancellationToken);
    }
}
