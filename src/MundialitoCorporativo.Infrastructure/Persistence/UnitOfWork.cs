using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Infrastructure.DomainEvents;
using MundialitoCorporativo.Infrastructure.Persistence.Configurations;

namespace MundialitoCorporativo.Infrastructure.Persistence;

/// <summary>
/// Unit of Work (requerimiento ítem 4). Es el único componente que guarda cambios en la base de datos:
/// abre la transacción, registra los eventos de dominio en EventosDominio, asigna la auditoría,
/// confirma todo junto y, ante cualquier fallo, revierte.
/// </summary>
internal sealed class UnitOfWork(
    MundialitoDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
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

        RegisterDomainEvents(domainEvents);
        ApplyAuditValues();

        var ownsTransaction = dbContext.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

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

        foreach (var (aggregate, domainEvent) in domainEvents)
        {
            logger.LogInformation(
                "Evento de dominio {EventType} {EventId} registrado para {AggregateType} {AggregateId}",
                domainEvent.GetType().Name,
                domainEvent.EventId,
                aggregate.GetType().Name,
                aggregate.Id);
        }

        return Result.Success();
    }

    private void RegisterDomainEvents(IEnumerable<(AggregateRoot Aggregate, IDomainEvent Event)> domainEvents)
    {
        var traceId = Activity.Current?.TraceId.ToHexString();

        foreach (var (aggregate, domainEvent) in domainEvents)
        {
            dbContext.EventosDominio.Add(new RegistroEventoDominio(
                domainEvent.EventId,
                domainEvent.GetType().Name,
                aggregate.GetType().Name,
                aggregate.Id,
                JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), EventSerializerOptions),
                domainEvent.OccurredOnUtc,
                traceId,
                correlationId: null));
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
}
