using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Application.Common.Observability;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Infrastructure.DomainEvents;
using MundialitoCorporativo.Infrastructure.Persistence.Configurations;

namespace MundialitoCorporativo.Infrastructure.Persistence;

/// <summary>
/// Unit of Work (requerimiento ítem 4). Es el único componente que guarda cambios en la base de datos:
/// abre la transacción, registra los eventos de dominio en EventosDominio, asigna la auditoría,
/// confirma todo junto y, ante cualquier fallo, revierte. Después del commit despacha los eventos
/// a sus handlers (requerimiento ítem 7).
/// </summary>
internal sealed partial class UnitOfWork(
    MundialitoDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IDomainEventDispatcher domainEventDispatcher,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    // Códigos de SQL Server: 2601/2627 índice o clave única duplicada, 547 conflicto con FK o CHECK.
    private static readonly HashSet<int> ConstraintViolationNumbers = [2601, 2627, 547];

    // Conserva tildes y eñes en DatosEvento para que el JSON sea legible desde SSMS.
    private static readonly JsonSerializerOptions EventSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public async Task<Result> CommitAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(aggregate => aggregate.DomainEvents.Select(domainEvent => (Aggregate: aggregate, Event: domainEvent)))
            .ToList();

        var registros = RegisterDomainEvents(domainEvents);
        ApplyAuditValues();

        var ownsTransaction = dbContext.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            await dbContext.SaveChangesFromUnitOfWorkAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await RollbackAsync(transaction, cancellationToken);
            logger.LogWarning(exception, "Conflicto de concurrencia al confirmar {EntityCount} entidades", exception.Entries.Count);

            return Error.Conflict(
                "El registro fue modificado por otro usuario. Vuelva a consultarlo e intente de nuevo.",
                "Database.ConcurrencyConflict");
        }
        catch (DbUpdateException exception) when (IsConstraintViolation(exception, out var sqlErrorNumber))
        {
            await RollbackAsync(transaction, cancellationToken);
            logger.LogWarning(exception, "Restricción de base de datos violada (SQL {SqlErrorNumber})", sqlErrorNumber);

            return Error.Conflict(
                "La operación entra en conflicto con datos existentes.",
                "Database.ConstraintViolation");
        }

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        await PublishDomainEventsAsync(domainEvents, registros);

        return Result.Success();
    }

    /// <summary>
    /// Cada evento queda en EventosDominio con el TraceId y el CorrelationId del request que lo originó.
    /// </summary>
    private List<RegistroEventoDominio> RegisterDomainEvents(IEnumerable<(AggregateRoot Aggregate, IDomainEvent Event)> domainEvents)
    {
        var traceId = ApplicationDiagnostics.CurrentTraceId;
        var correlationId = ApplicationDiagnostics.CurrentCorrelationId;
        var registros = new List<RegistroEventoDominio>();

        foreach (var (aggregate, domainEvent) in domainEvents)
        {
            var registro = new RegistroEventoDominio(
                domainEvent.EventId,
                domainEvent.GetType().Name,
                aggregate.GetType().Name,
                aggregate.Id,
                JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), EventSerializerOptions),
                domainEvent.OccurredOnUtc,
                traceId,
                correlationId);

            dbContext.EventosDominio.Add(registro);
            registros.Add(registro);
        }

        return registros;
    }

    /// <summary>
    /// Los datos ya están confirmados: un handler que falla no revierte la operación ni cambia la respuesta.
    /// Su evento queda sin FechaProcesado y el fallo se registra con nivel Error.
    /// Se usa CancellationToken.None para que los handlers corran aunque el cliente cancele la petición.
    /// </summary>
    private async Task PublishDomainEventsAsync(
        IReadOnlyList<(AggregateRoot Aggregate, IDomainEvent Event)> domainEvents,
        IReadOnlyList<RegistroEventoDominio> registros)
    {
        if (domainEvents.Count == 0)
        {
            return;
        }

        var processedCount = 0;
        for (var index = 0; index < domainEvents.Count; index++)
        {
            var (aggregate, domainEvent) = domainEvents[index];
            var eventType = domainEvent.GetType().Name;

            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["DomainEventId"] = domainEvent.EventId,
                ["DomainEventType"] = eventType
            });

            LogDomainEventRegistered(logger, eventType, domainEvent.EventId, aggregate.GetType().Name, aggregate.Id);

            try
            {
                await domainEventDispatcher.DispatchAsync(domainEvent, CancellationToken.None);
                registros[index].MarkAsProcessed(dateTimeProvider.UtcNow);
                processedCount++;
            }
            catch (Exception exception)
            {
                LogDomainEventHandlerFailed(logger, exception, eventType, domainEvent.EventId);
            }
        }

        if (processedCount == 0)
        {
            return;
        }

        try
        {
            await dbContext.SaveChangesFromUnitOfWorkAsync(CancellationToken.None);
        }
        catch (DbUpdateException exception)
        {
            LogProcessedDateNotSaved(logger, exception, processedCount);
        }
    }

    private void ApplyAuditValues()
    {
        var utcNow = dateTimeProvider.UtcNow;

        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Metadata.FindProperty(AuditColumns.FechaCreacion) is not null)
            {
                entry.Property(AuditColumns.FechaCreacion).CurrentValue = utcNow;
            }

            if (entry.State == EntityState.Modified && entry.Metadata.FindProperty(AuditColumns.FechaModificacion) is not null)
            {
                entry.Property(AuditColumns.FechaModificacion).CurrentValue = utcNow;
            }
        }
    }

    private static async Task RollbackAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    private static bool IsConstraintViolation(DbUpdateException exception, out int sqlErrorNumber)
    {
        sqlErrorNumber = exception.InnerException is SqlException sqlException ? sqlException.Number : 0;
        return ConstraintViolationNumbers.Contains(sqlErrorNumber);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Evento de dominio {DomainEventType} {DomainEventId} registrado para {AggregateType} {AggregateId}")]
    private static partial void LogDomainEventRegistered(ILogger logger, string domainEventType, Guid domainEventId, string aggregateType, Guid aggregateId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Falló un handler del evento de dominio {DomainEventType} {DomainEventId}; el evento queda pendiente")]
    private static partial void LogDomainEventHandlerFailed(ILogger logger, Exception exception, string domainEventType, Guid domainEventId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo guardar FechaProcesado de {ProcessedCount} eventos de dominio")]
    private static partial void LogProcessedDateNotSaved(ILogger logger, Exception exception, int processedCount);
}
