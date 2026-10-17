namespace MundialitoCorporativo.Application.Abstractions.Idempotency;

public enum EstadoRegistroIdempotencia
{
    EnProceso = 0,
    Completado = 1
}

/// <summary>
/// Registro de una Idempotency-Key ya utilizada (tabla dbo.RegistrosIdempotencia).
/// Guarda la respuesta original para devolverla igual cuando el POST se repite.
/// </summary>
public sealed record RegistroIdempotencia
{
    public string ClaveIdempotencia { get; init; } = string.Empty;

    public string RutaSolicitud { get; init; } = string.Empty;

    public string MetodoSolicitud { get; init; } = string.Empty;

    /// <summary>SHA-256 (hexadecimal) del cuerpo de la petición original.</summary>
    public string HashSolicitud { get; init; } = string.Empty;

    public EstadoRegistroIdempotencia Estado { get; init; }

    public int? CodigoEstadoRespuesta { get; init; }

    public string? CuerpoRespuesta { get; init; }

    /// <summary>JSON con los encabezados de la respuesta que deben repetirse (Location, Content-Type).</summary>
    public string? EncabezadosRespuesta { get; init; }

    public DateTime FechaCreacion { get; init; }

    public DateTime? FechaCompletado { get; init; }

    public DateTime FechaExpiracion { get; init; }
}
