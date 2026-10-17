using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using MundialitoCorporativo.Application.Common.Observability;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Equipos.Events;
using MundialitoCorporativo.Domain.Torneos;
using MundialitoCorporativo.Infrastructure.Persistence;
using MundialitoCorporativo.Infrastructure.Persistence.Repositories;
using MundialitoCorporativo.UnitTests.Domain;

namespace MundialitoCorporativo.UnitTests.Infrastructure;

/// <summary>
/// Prueba mínima del ítem 15: comportamiento del commit del UnitOfWork.
/// Usa el proveedor en memoria de EF Core; las pruebas contra SQL Server real están en IntegrationTests.
/// </summary>
public class UnitOfWorkTests
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly RecordingDomainEventDispatcher _domainEventDispatcher = new();
    private readonly FakeLogger<UnitOfWork> _logger = new();

    [Fact]
    public async Task CommitAsync_ShouldPersistChangesFromMultipleRepositoriesTogether()
    {
        await using (var context = CreateContext())
        {
            var equipo = TestData.NewEquipo("Tecnología FC", "TEC");
            var torneo = TestData.NewTorneo();
            torneo.RegisterEquipo(equipo.Id, TestData.UtcNow);

            new EquipoRepository(context).Add(equipo);
            new TorneoRepository(context).Add(torneo);

            var result = await CreateUnitOfWork(context).CommitAsync();

            Assert.True(result.IsSuccess);
        }

        await using var verification = CreateContext();
        Assert.Equal(1, await verification.Equipos.CountAsync());
        var torneoGuardado = await verification.Torneos.Include(torneo => torneo.Equipos).SingleAsync();
        Assert.Single(torneoGuardado.Equipos);
    }

    [Fact]
    public async Task WithoutCommit_ShouldNotPersistChanges()
    {
        await using (var context = CreateContext())
        {
            new EquipoRepository(context).Add(TestData.NewEquipo("Tecnología FC", "TEC"));
        }

        await using var verification = CreateContext();
        Assert.Equal(0, await verification.Equipos.CountAsync());
    }

    [Fact]
    public async Task CommitAsync_ShouldRegisterDomainEventsAndClearThem()
    {
        await using var context = CreateContext();
        var equipo = TestData.NewEquipo("Tecnología FC", "TEC");
        new EquipoRepository(context).Add(equipo);

        var result = await CreateUnitOfWork(context).CommitAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(equipo.DomainEvents);

        var registro = Assert.Single(await context.EventosDominio.ToListAsync());
        Assert.Equal("EquipoCreatedDomainEvent", registro.TipoEvento);
        Assert.Equal(nameof(Equipo), registro.TipoAgregado);
        Assert.Equal(equipo.Id, registro.AgregadoId);
        Assert.Contains(equipo.Nombre, registro.DatosEvento, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CommitAsync_ShouldAssignFechaCreacionFromDateTimeProvider()
    {
        await using var context = CreateContext();
        var equipo = TestData.NewEquipo("Tecnología FC", "TEC");
        new EquipoRepository(context).Add(equipo);

        await CreateUnitOfWork(context).CommitAsync();

        Assert.Equal(TestData.UtcNow, context.Entry(equipo).Property("FechaCreacion").CurrentValue);
    }

    [Fact]
    public async Task CommitAsync_ShouldAssignFechaModificacionOnUpdate()
    {
        var equipo = TestData.NewEquipo("Tecnología FC", "TEC");
        await using (var context = CreateContext())
        {
            new EquipoRepository(context).Add(equipo);
            await CreateUnitOfWork(context).CommitAsync();
        }

        var later = TestData.UtcNow.AddHours(2);
        await using var updateContext = CreateContext();
        var stored = await new EquipoRepository(updateContext).GetByIdAsync(equipo.Id);
        stored!.Update("Tecnología United", "TU", null);

        await CreateUnitOfWork(updateContext, later).CommitAsync();

        Assert.Equal(later, updateContext.Entry(stored).Property("FechaModificacion").CurrentValue);
    }

    [Fact]
    public async Task CommitAsync_OnConcurrencyConflict_ShouldReturnConflictAndKeepDomainEvents()
    {
        await using var context = CreateContext(new FailingSaveInterceptor(new DbUpdateConcurrencyException("Conflicto simulado")));
        var equipo = TestData.NewEquipo("Tecnología FC", "TEC");
        new EquipoRepository(context).Add(equipo);

        var result = await CreateUnitOfWork(context).CommitAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Database.ConcurrencyConflict", result.ErrorCode);
        Assert.Single(equipo.DomainEvents);
        Assert.Empty(_domainEventDispatcher.Dispatched);
    }

    [Fact]
    public async Task CommitAsync_OnUnexpectedError_ShouldPropagateExceptionAndKeepDomainEvents()
    {
        await using var context = CreateContext(new FailingSaveInterceptor(new InvalidOperationException("Base de datos no disponible")));
        var equipo = TestData.NewEquipo("Tecnología FC", "TEC");
        new EquipoRepository(context).Add(equipo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateUnitOfWork(context).CommitAsync());

        Assert.Single(equipo.DomainEvents);
        Assert.Empty(_domainEventDispatcher.Dispatched);
    }

    [Fact]
    public async Task CommitAsync_ShouldDispatchDomainEventsAfterCommitAndMarkThemProcessed()
    {
        await using var context = CreateContext();
        var equipo = TestData.NewEquipo("Tecnología FC", "TEC");
        new EquipoRepository(context).Add(equipo);

        var savedBeforeDispatch = false;
        _domainEventDispatcher.OnDispatch = _ =>
        {
            using var verification = CreateContext();
            savedBeforeDispatch = verification.Equipos.Any(stored => stored.Id == equipo.Id);
        };

        var later = TestData.UtcNow.AddMinutes(5);
        var result = await CreateUnitOfWork(context, later).CommitAsync();

        Assert.True(result.IsSuccess);
        Assert.True(savedBeforeDispatch, "Los eventos se despachan solo cuando los datos ya están confirmados.");
        var dispatched = Assert.IsType<EquipoCreatedDomainEvent>(Assert.Single(_domainEventDispatcher.Dispatched));
        Assert.Equal(equipo.Id, dispatched.EquipoId);

        await using var stored = CreateContext();
        Assert.Equal(later, (await stored.EventosDominio.SingleAsync()).FechaProcesado);

        var log = Assert.Single(_logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Information);
        Assert.Equal(dispatched.EventId.ToString(), log.GetStructuredStateValue("DomainEventId"));
        Assert.Equal(nameof(EquipoCreatedDomainEvent), log.GetStructuredStateValue("DomainEventType"));
    }

    [Fact]
    public async Task CommitAsync_WhenDomainEventHandlerFails_ShouldSucceedAndLeaveEventPending()
    {
        await using var context = CreateContext();
        new EquipoRepository(context).Add(TestData.NewEquipo("Tecnología FC", "TEC"));
        _domainEventDispatcher.FailWith = new InvalidOperationException("Handler con error");

        var result = await CreateUnitOfWork(context).CommitAsync();

        Assert.True(result.IsSuccess);
        await using var stored = CreateContext();
        Assert.Equal(1, await stored.Equipos.CountAsync());
        Assert.Null((await stored.EventosDominio.SingleAsync()).FechaProcesado);

        var log = Assert.Single(_logger.Collector.GetSnapshot(), record => record.Level == LogLevel.Error);
        Assert.IsType<InvalidOperationException>(log.Exception);
    }

    [Fact]
    public async Task CommitAsync_ShouldStoreTraceIdAndCorrelationIdOfCurrentRequest()
    {
        using var activity = new Activity("POST /api/equipos")
            .SetIdFormat(ActivityIdFormat.W3C)
            .AddBaggage(ApplicationDiagnostics.CorrelationIdBaggageKey, "correlacion-demo-1")
            .Start();

        await using var context = CreateContext();
        new EquipoRepository(context).Add(TestData.NewEquipo("Tecnología FC", "TEC"));

        var result = await CreateUnitOfWork(context).CommitAsync();

        Assert.True(result.IsSuccess);
        var registro = await context.EventosDominio.SingleAsync();
        Assert.Equal(activity.TraceId.ToHexString(), registro.TraceId);
        Assert.Equal("correlacion-demo-1", registro.CorrelationId);
    }

    [Fact]
    public async Task CommitAsync_WithoutChanges_ShouldSucceed()
    {
        await using var context = CreateContext();

        var result = await CreateUnitOfWork(context).CommitAsync();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void DbContextSaveChanges_OutsideUnitOfWork_ShouldThrow()
    {
        using var context = CreateContext();
        context.Equipos.Add(TestData.NewEquipo("Tecnología FC", "TEC"));

        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
    }

    [Fact]
    public async Task DbContextSaveChangesAsync_OutsideUnitOfWork_ShouldThrowAndNotPersist()
    {
        await using (var context = CreateContext())
        {
            context.Equipos.Add(TestData.NewEquipo("Tecnología FC", "TEC"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }

        await using var verification = CreateContext();
        Assert.Equal(0, await verification.Equipos.CountAsync());
    }

    private MundialitoDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<MundialitoDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(interceptors)
            .Options;

        return new MundialitoDbContext(options);
    }

    private UnitOfWork CreateUnitOfWork(MundialitoDbContext context, DateTime? utcNow = null) =>
        new(context, new FixedDateTimeProvider(utcNow ?? TestData.UtcNow), _domainEventDispatcher, _logger);

    private sealed class FailingSaveInterceptor(Exception exception) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw exception;
    }
}
