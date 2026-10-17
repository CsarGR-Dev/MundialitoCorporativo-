using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MundialitoCorporativo.Application.Abstractions.Idempotency;

namespace MundialitoCorporativo.Infrastructure.Idempotency;

/// <summary>
/// Elimina periódicamente las Idempotency-Key vencidas para que la tabla no crezca sin límite.
/// </summary>
internal sealed partial class IdempotencyCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<IdempotencyOptions> options,
    ILogger<IdempotencyCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.CleanupInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var store = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

                var deleted = await store.DeleteExpiredAsync(stoppingToken);
                if (deleted > 0)
                {
                    LogExpiredDeleted(logger, deleted);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Un fallo de limpieza no debe detener la API; se reintenta en el siguiente ciclo.
                LogCleanupFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Limpieza de idempotencia: {DeletedCount} claves vencidas eliminadas")]
    private static partial void LogExpiredDeleted(ILogger logger, int deletedCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo limpiar las claves de idempotencia vencidas")]
    private static partial void LogCleanupFailed(ILogger logger, Exception exception);
}
