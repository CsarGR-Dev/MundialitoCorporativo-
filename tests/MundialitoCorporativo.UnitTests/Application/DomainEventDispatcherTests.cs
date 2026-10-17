using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using MundialitoCorporativo.Application;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Domain.Equipos.Events;
using MundialitoCorporativo.Domain.Jugadores.Events;
using MundialitoCorporativo.Domain.Partidos.Events;

namespace MundialitoCorporativo.UnitTests.Application;

/// <summary>
/// Eventos de dominio obligatorios del ítem 7 (equipo creado y resultado registrado): llegan a sus handlers
/// y quedan en el log con propiedades estructuradas.
/// </summary>
public class DomainEventDispatcherTests
{
    private readonly IServiceProvider _services;
    private readonly FakeLogCollector _logs;

    public DomainEventDispatcherTests()
    {
        var provider = new ServiceCollection()
            .AddFakeLogging()
            .AddApplication()
            .BuildServiceProvider();

        _services = provider.CreateScope().ServiceProvider;
        _logs = provider.GetFakeLogCollector();
    }

    private IDomainEventDispatcher Dispatcher => _services.GetRequiredService<IDomainEventDispatcher>();

    [Fact]
    public void AddApplication_ShouldRegisterHandlersForMandatoryEvents()
    {
        Assert.Single(_services.GetServices<IDomainEventHandler<EquipoCreatedDomainEvent>>());
        Assert.Single(_services.GetServices<IDomainEventHandler<PartidoResultRegisteredDomainEvent>>());
    }

    [Fact]
    public async Task EquipoCreated_ShouldBeLoggedWithStructuredProperties()
    {
        var domainEvent = new EquipoCreatedDomainEvent(Guid.NewGuid(), "Tecnología FC", "TEC");

        await Dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        var record = Assert.Single(_logs.GetSnapshot());
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal(domainEvent.EquipoId.ToString(), record.GetStructuredStateValue("EquipoId"));
        Assert.Equal("TEC", record.GetStructuredStateValue("Abreviatura"));
    }

    [Fact]
    public async Task PartidoResultRegistered_ShouldBeLoggedWithScore()
    {
        var domainEvent = new PartidoResultRegisteredDomainEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, 1);

        await Dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        var record = Assert.Single(_logs.GetSnapshot());
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal(domainEvent.PartidoId.ToString(), record.GetStructuredStateValue("PartidoId"));
        Assert.Equal(domainEvent.TorneoId.ToString(), record.GetStructuredStateValue("TorneoId"));
        Assert.Equal("2", record.GetStructuredStateValue("GolesLocal"));
        Assert.Equal("1", record.GetStructuredStateValue("GolesVisitante"));
    }

    [Fact]
    public async Task EventWithoutHandlers_ShouldCompleteWithoutErrors()
    {
        await Dispatcher.DispatchAsync(new JugadorRegisteredDomainEvent(Guid.NewGuid(), Guid.NewGuid(), 10), CancellationToken.None);

        Assert.Empty(_logs.GetSnapshot());
    }

    [Fact]
    public async Task Dispatch_ShouldMarkTheEventInTheCurrentTrace()
    {
        using var request = new Activity("POST /api/equipos").SetIdFormat(ActivityIdFormat.W3C).Start();
        var domainEvent = new EquipoCreatedDomainEvent(Guid.NewGuid(), "Tecnología FC", "TEC");

        await Dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        var traceEvent = Assert.Single(request.Events);
        Assert.Equal(nameof(EquipoCreatedDomainEvent), traceEvent.Name);
        Assert.Contains(traceEvent.Tags, tag => tag.Key == "mundialito.event.id" && Equals(tag.Value, domainEvent.EventId.ToString()));
    }
}
