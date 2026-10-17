using Dapper;
using Microsoft.Extensions.Options;
using MundialitoCorporativo.Application.Abstractions.Data;
using MundialitoCorporativo.Application.Abstractions.Idempotency;
using MundialitoCorporativo.Application.Abstractions.Time;

namespace MundialitoCorporativo.Infrastructure.Idempotency;

/// <summary>
/// Almacena las Idempotency-Key en dbo.RegistrosIdempotencia con SQL directo (Dapper), fuera del Unit of Work:
/// la reserva queda confirmada antes de ejecutar el Command y no depende de su transacción.
/// </summary>
internal sealed class SqlIdempotencyStore(
    IDbConnectionFactory connectionFactory,
    IDateTimeProvider dateTimeProvider,
    IOptions<IdempotencyOptions> options) : IIdempotencyStore
{
    private const int CleanupBatchSize = 1000;

    // UPDLOCK + HOLDLOCK bloquean la clave (aunque todavía no exista) hasta el COMMIT:
    // dos peticiones concurrentes con la misma clave se serializan y solo una obtiene la reserva.
    private const string TryBeginSql = """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        -- Una clave vencida, o una reserva abandonada, puede volver a usarse.
        DELETE FROM dbo.RegistrosIdempotencia WITH (HOLDLOCK)
         WHERE ClaveIdempotencia = @Clave AND RutaSolicitud = @Ruta
           AND (FechaExpiracion <= @Ahora OR (Estado = 'EnProceso' AND FechaCreacion <= @LimiteEnProceso));

        INSERT INTO dbo.RegistrosIdempotencia
               (ClaveIdempotencia, RutaSolicitud, MetodoSolicitud, HashSolicitud, Estado, FechaCreacion, FechaExpiracion)
        SELECT @Clave, @Ruta, @Metodo, @Hash, 'EnProceso', @Ahora, @FechaExpiracion
         WHERE NOT EXISTS (SELECT 1 FROM dbo.RegistrosIdempotencia WITH (UPDLOCK, HOLDLOCK)
                            WHERE ClaveIdempotencia = @Clave AND RutaSolicitud = @Ruta);

        IF @@ROWCOUNT = 0
            SELECT ClaveIdempotencia, RutaSolicitud, MetodoSolicitud, HashSolicitud, Estado, CodigoEstadoRespuesta,
                   CuerpoRespuesta, EncabezadosRespuesta, FechaCreacion, FechaCompletado, FechaExpiracion
              FROM dbo.RegistrosIdempotencia
             WHERE ClaveIdempotencia = @Clave AND RutaSolicitud = @Ruta;

        COMMIT TRANSACTION;
        """;

    private const string CompleteSql = """
        UPDATE dbo.RegistrosIdempotencia
           SET Estado = 'Completado', CodigoEstadoRespuesta = @Codigo, CuerpoRespuesta = @Cuerpo,
               EncabezadosRespuesta = @Encabezados, FechaCompletado = @Ahora
         WHERE ClaveIdempotencia = @Clave AND RutaSolicitud = @Ruta AND Estado = 'EnProceso';
        """;

    private const string ReleaseSql = """
        DELETE FROM dbo.RegistrosIdempotencia
         WHERE ClaveIdempotencia = @Clave AND RutaSolicitud = @Ruta AND Estado = 'EnProceso';
        """;

    private const string DeleteExpiredSql = """
        DELETE TOP (@Lote) FROM dbo.RegistrosIdempotencia WHERE FechaExpiracion <= @Ahora;
        """;

    public async Task<RegistroIdempotencia?> TryBeginAsync(IdempotencyRequest request, CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;
        var settings = options.Value;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<RegistroIdempotencia>(new CommandDefinition(
            TryBeginSql,
            new
            {
                Clave = request.Key,
                Ruta = request.Path,
                Metodo = request.Method,
                Hash = request.RequestHash,
                Ahora = now,
                LimiteEnProceso = now - settings.InProgressTimeout,
                FechaExpiracion = now + settings.Expiration
            },
            cancellationToken: cancellationToken));
    }

    public async Task CompleteAsync(string key, string path, IdempotencyResponse response, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            CompleteSql,
            new
            {
                Clave = key,
                Ruta = path,
                Codigo = (short)response.StatusCode,
                Cuerpo = response.Body,
                Encabezados = response.Headers,
                Ahora = dateTimeProvider.UtcNow
            },
            cancellationToken: cancellationToken));
    }

    public async Task ReleaseAsync(string key, string path, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            ReleaseSql, new { Clave = key, Ruta = path }, cancellationToken: cancellationToken));
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var total = 0;
        int deleted;
        do
        {
            deleted = await connection.ExecuteAsync(new CommandDefinition(
                DeleteExpiredSql,
                new { Lote = CleanupBatchSize, Ahora = dateTimeProvider.UtcNow },
                cancellationToken: cancellationToken));
            total += deleted;
        }
        while (deleted == CleanupBatchSize);

        return total;
    }
}
