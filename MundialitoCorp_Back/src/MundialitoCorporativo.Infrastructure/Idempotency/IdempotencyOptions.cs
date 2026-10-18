namespace MundialitoCorporativo.Infrastructure.Idempotency;

/// <summary>
/// Configuración de la persistencia de Idempotency-Key (sección "Idempotency" de appsettings).
/// </summary>
public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    /// <summary>Tiempo durante el cual una clave devuelve la respuesta original.</summary>
    public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Tiempo tras el cual una reserva EnProceso se considera abandonada (por ejemplo, si la API se detuvo
    /// a mitad de la petición) y la clave puede volver a usarse.
    /// </summary>
    public TimeSpan InProgressTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Frecuencia de la limpieza de registros vencidos.</summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
