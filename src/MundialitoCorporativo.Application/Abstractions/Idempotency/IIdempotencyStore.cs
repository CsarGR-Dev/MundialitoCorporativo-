namespace MundialitoCorporativo.Application.Abstractions.Idempotency;

/// <summary>
/// Persistencia de las Idempotency-Key (requerimiento ítem 6). Se implementa fuera del Unit of Work de negocio
/// para que el registro exista aunque la transacción del Command falle.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Intenta reservar la clave para la ruta (Path). Devuelve <c>null</c> si la reserva se obtuvo (registro nuevo
    /// en estado EnProceso) o el registro existente si la clave ya fue usada. Las claves vencidas se reemplazan.
    /// </summary>
    Task<RegistroIdempotencia?> TryBeginAsync(IdempotencyRequest request, CancellationToken cancellationToken);

    /// <summary>Guarda la respuesta original y marca el registro como Completado.</summary>
    Task CompleteAsync(string key, string path, IdempotencyResponse response, CancellationToken cancellationToken);

    /// <summary>Elimina una reserva EnProceso para permitir reintentar con la misma clave (por ejemplo, tras un 5xx).</summary>
    Task ReleaseAsync(string key, string path, CancellationToken cancellationToken);

    /// <summary>Elimina los registros vencidos y devuelve cuántos se borraron.</summary>
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken);
}

public sealed record IdempotencyRequest(string Key, string Path, string Method, string RequestHash);

public sealed record IdempotencyResponse(int StatusCode, string? Body, string? Headers);
