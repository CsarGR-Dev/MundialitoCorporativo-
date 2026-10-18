using System.Diagnostics;

namespace MundialitoCorporativo.Application.Common.Observability;

/// <summary>
/// Trazabilidad compartida por todas las capas (requerimiento ítem 7). El TraceId es el de W3C Trace Context
/// de la petición en curso y el CorrelationId viaja como baggage de la misma traza, así llega a Application
/// e Infrastructure sin depender de HTTP.
/// </summary>
public static class ApplicationDiagnostics
{
    public const string ActivitySourceName = "MundialitoCorporativo.Application";

    public const string CorrelationIdBaggageKey = "correlation.id";

    /// <summary>Origen de los spans propios: uno por Command o Query despachado.</summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static string? CurrentTraceId =>
        Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity ? activity.TraceId.ToHexString() : null;

    public static string? CurrentCorrelationId => Activity.Current?.GetBaggageItem(CorrelationIdBaggageKey);
}
