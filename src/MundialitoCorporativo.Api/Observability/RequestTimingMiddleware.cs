using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace MundialitoCorporativo.Api.Observability;

/// <summary>
/// Registra la duración de cada request (requerimiento ítem 7) con un nivel según el resultado:
/// 2xx/3xx Information, 4xx Warning, 5xx Error y Warning si supera el umbral de request lento.
/// Solo se registra la ruta, sin query string ni cuerpo, para no exponer datos de la petición.
/// </summary>
public sealed partial class RequestTimingMiddleware(
    RequestDelegate next,
    IOptions<ObservabilityOptions> options,
    ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            // Solo llega aquí si el manejador de excepciones no pudo responder (por ejemplo, respuesta ya iniciada).
            LogRequestFailed(logger, exception, context.Request.Method, context.Request.Path.Value, ElapsedMs(startedAt));
            throw;
        }

        var elapsedMs = ElapsedMs(startedAt);
        var statusCode = context.Response.StatusCode;
        var thresholdMs = options.Value.SlowRequestThreshold.TotalMilliseconds;
        var method = context.Request.Method;
        var path = context.Request.Path.Value;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogServerError(logger, method, path, statusCode, elapsedMs);
        }
        else if (statusCode >= StatusCodes.Status400BadRequest)
        {
            LogClientError(logger, method, path, statusCode, elapsedMs);
        }
        else if (elapsedMs > thresholdMs)
        {
            LogSlowRequest(logger, method, path, statusCode, elapsedMs, thresholdMs);
        }
        else
        {
            LogRequestCompleted(logger, method, path, statusCode, elapsedMs);
        }
    }

    private static double ElapsedMs(long startedAt) => Math.Round(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds, 1);

    private const string Template = "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {ElapsedMs} ms";

    [LoggerMessage(Level = LogLevel.Information, Message = Template)]
    private static partial void LogRequestCompleted(ILogger logger, string requestMethod, string? requestPath, int statusCode, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = Template)]
    private static partial void LogClientError(ILogger logger, string requestMethod, string? requestPath, int statusCode, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = Template)]
    private static partial void LogServerError(ILogger logger, string requestMethod, string? requestPath, int statusCode, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = Template + " (request lento, umbral {ThresholdMs} ms)")]
    private static partial void LogSlowRequest(ILogger logger, string requestMethod, string? requestPath, int statusCode, double elapsedMs, double thresholdMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP {RequestMethod} {RequestPath} falló sin respuesta en {ElapsedMs} ms")]
    private static partial void LogRequestFailed(ILogger logger, Exception exception, string requestMethod, string? requestPath, double elapsedMs);
}
