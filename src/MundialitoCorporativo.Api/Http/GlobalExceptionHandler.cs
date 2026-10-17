using Microsoft.AspNetCore.Diagnostics;

namespace MundialitoCorporativo.Api.Http;

/// <summary>
/// Último recurso para errores inesperados: las reglas de negocio nunca llegan aquí porque viajan como Result.
/// Registra la excepción con nivel Error y responde 500 sin exponer detalles internos.
/// </summary>
internal sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        LogUnhandledException(logger, httpContext.Request.Method, httpContext.Request.Path.Value, exception);

        await ApiProblemDetails.WriteAsync(httpContext, ApiProblemDetails.UnexpectedError, exception);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error no controlado en {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, string method, string? path, Exception exception);
}
