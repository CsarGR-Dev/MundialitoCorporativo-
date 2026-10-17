using System.Diagnostics;
using MundialitoCorporativo.Application.Common.Observability;

namespace MundialitoCorporativo.Api.Observability;

/// <summary>
/// Trazabilidad por request (requerimiento ítem 7):
/// <list type="bullet">
/// <item>CorrelationId: se toma del header <c>X-Correlation-Id</c> o se genera uno nuevo.</item>
/// <item>TraceId: el de W3C Trace Context (respeta un <c>traceparent</c> entrante).</item>
/// </list>
/// Ambos se devuelven en los headers de la respuesta y se agregan a todos los logs del request.
/// El CorrelationId viaja además como baggage de la traza para que lo lean Application e Infrastructure.
/// </summary>
public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string TraceIdHeaderName = "X-Trace-Id";

    /// <summary>Mismo largo que la columna EventosDominio.CorrelationId.</summary>
    public const int MaxLength = 64;

    private const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[HeaderName].ToString());
        context.Items[ItemKey] = correlationId;

        Activity.Current?.SetBaggage(ApplicationDiagnostics.CorrelationIdBaggageKey, correlationId);
        Activity.Current?.SetTag(ApplicationDiagnostics.CorrelationIdBaggageKey, correlationId);

        var traceId = ApplicationDiagnostics.CurrentTraceId ?? context.TraceIdentifier;

        // OnStarting: los headers se agregan también a las respuestas que reescribe el manejador de excepciones.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            context.Response.Headers[TraceIdHeaderName] = traceId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId
        }))
        {
            await next(context);
        }
    }

    public static string? GetCorrelationId(HttpContext context) =>
        context.Items.TryGetValue(ItemKey, out var value) ? value as string : null;

    /// <summary>Se acepta un valor de 1 a 64 caracteres ASCII visibles; cualquier otro se reemplaza.</summary>
    internal static bool IsValid(string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= MaxLength
        && value.All(character => character is >= '!' and <= '~');

    private string ResolveCorrelationId(string received)
    {
        if (IsValid(received))
        {
            return received;
        }

        if (received.Length > 0)
        {
            LogInvalidCorrelationId(logger, received.Length);
        }

        return Guid.NewGuid().ToString();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "X-Correlation-Id inválido ({Length} caracteres); se genera uno nuevo")]
    private static partial void LogInvalidCorrelationId(ILogger logger, int length);
}
