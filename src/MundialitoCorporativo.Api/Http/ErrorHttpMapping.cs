using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Api.Http;

/// <summary>
/// Traducción del tipo de error del Result Pattern al código HTTP (requerimiento ítem 5).
/// </summary>
public static class ErrorHttpMapping
{
    public static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
