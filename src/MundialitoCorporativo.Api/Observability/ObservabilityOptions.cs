namespace MundialitoCorporativo.Api.Observability;

/// <summary>
/// Configuración de logs y trazas (sección "Observability" de appsettings).
/// </summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    /// <summary>Nombre del servicio en los logs y en las trazas.</summary>
    public string ServiceName { get; init; } = "mundialito-api";

    /// <summary>Formato de la consola: "Json" (contenedores) o "Text" (desarrollo).</summary>
    public string ConsoleFormat { get; init; } = "Json";

    /// <summary>Archivo de log diario en JSON. Vacío desactiva el archivo.</summary>
    public string? LogFilePath { get; init; }

    public int RetainedLogFiles { get; init; } = 7;

    /// <summary>Un request que tarda más que esto se registra como Warning.</summary>
    public TimeSpan SlowRequestThreshold { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Endpoint OTLP (por ejemplo, Jaeger en http://localhost:4317). Vacío desactiva la exportación de trazas.</summary>
    public string? OtlpEndpoint { get; init; }
}
