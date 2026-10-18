using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Messaging;

/// <summary>
/// Despacho en proceso de eventos de dominio (sin bus externo): resuelve los handlers del tipo de evento
/// y deja el evento marcado en la traza actual para verlo junto al request que lo originó.
/// </summary>
internal sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, IDomainEventExecutor> Executors = new();

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();

        Activity.Current?.AddEvent(new ActivityEvent(
            eventType.Name,
            tags: new ActivityTagsCollection { ["mundialito.event.id"] = domainEvent.EventId.ToString() }));

        var executor = Executors.GetOrAdd(
            eventType,
            type => (IDomainEventExecutor)Activator.CreateInstance(typeof(DomainEventExecutor<>).MakeGenericType(type))!);

        return executor.Execute(domainEvent, serviceProvider, cancellationToken);
    }

    private interface IDomainEventExecutor
    {
        Task Execute(IDomainEvent domainEvent, IServiceProvider provider, CancellationToken cancellationToken);
    }

    private sealed class DomainEventExecutor<TEvent> : IDomainEventExecutor
        where TEvent : IDomainEvent
    {
        public async Task Execute(IDomainEvent domainEvent, IServiceProvider provider, CancellationToken cancellationToken)
        {
            foreach (var handler in provider.GetServices<IDomainEventHandler<TEvent>>())
            {
                await handler.Handle((TEvent)domainEvent, cancellationToken);
            }
        }
    }
}
