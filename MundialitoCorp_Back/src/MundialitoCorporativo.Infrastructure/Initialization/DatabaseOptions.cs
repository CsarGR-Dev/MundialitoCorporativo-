namespace MundialitoCorporativo.Infrastructure.Initialization;

/// <summary>
/// Preparación de la base de datos al iniciar la API (sección "Database" de appsettings).
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Crea la base y su esquema con los scripts de Initialization/Scripts (re-ejecutables) si aún no existen.</summary>
    public bool ApplySchemaOnStartup { get; init; } = true;

    /// <summary>Carga los datos iniciales del requerimiento (ítem 9) si la base está vacía.</summary>
    public bool SeedOnStartup { get; init; } = true;

    /// <summary>Intentos de conexión a SQL Server; en Docker la base puede tardar en estar lista.</summary>
    public int ConnectionRetries { get; init; } = 10;

    public TimeSpan ConnectionRetryDelay { get; init; } = TimeSpan.FromSeconds(5);
}
