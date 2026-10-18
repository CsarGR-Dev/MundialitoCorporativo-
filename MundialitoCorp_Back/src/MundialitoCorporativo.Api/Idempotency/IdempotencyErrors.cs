using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Api.Idempotency;

public static class IdempotencyErrors
{
    public static readonly Error KeyRequired = Error.Validation(
        $"El encabezado {IdempotencyMiddleware.HeaderName} es obligatorio en las peticiones POST.",
        "Idempotency.KeyRequired");

    public static readonly Error KeyInvalid = Error.Validation(
        $"El encabezado {IdempotencyMiddleware.HeaderName} debe ser un único valor de 1 a {IdempotencyMiddleware.MaxKeyLength} " +
        "caracteres visibles, sin espacios (se recomienda un GUID).",
        "Idempotency.KeyInvalid");

    public static readonly Error KeyReused = Error.Conflict(
        $"La {IdempotencyMiddleware.HeaderName} ya se usó con un cuerpo distinto en esta ruta. Use una clave nueva para otra operación.",
        "Idempotency.KeyReused");

    public static readonly Error InProgress = Error.Conflict(
        $"Ya hay una petición en curso con la misma {IdempotencyMiddleware.HeaderName}. Reintente en unos segundos.",
        "Idempotency.InProgress");
}
