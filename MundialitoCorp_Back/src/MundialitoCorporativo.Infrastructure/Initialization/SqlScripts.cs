using System.Text.RegularExpressions;

namespace MundialitoCorporativo.Infrastructure.Initialization;

/// <summary>
/// Scripts de estructura de Initialization/Scripts, incluidos en el ensamblado. Son la única definición del esquema:
/// los usan el arranque de la API, Docker y las pruebas de integración.
/// </summary>
internal static partial class SqlScripts
{
    /// <summary>Nombre de la base con el que están escritos los scripts.</summary>
    public const string ScriptDatabaseName = "Mundialito";

    public const string CrearBaseDatos = "001_DDL_CrearBaseDatos.sql";
    public const string VistasTablaPosicionesGoleadores = "002_VIEW_TablaPosicionesGoleadores.sql";

    /// <summary>Orden de ejecución para dejar una base lista.</summary>
    public static readonly IReadOnlyList<string> Schema = [CrearBaseDatos, VistasTablaPosicionesGoleadores];

    public static string Load(string fileName)
    {
        using var stream = typeof(SqlScripts).Assembly.GetManifestResourceStream($"Scripts.{fileName}")
            ?? throw new InvalidOperationException($"El script {fileName} no está incluido en el ensamblado.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Adapta el script a otra base (por ejemplo, las bases temporales de las pruebas).
    /// El nombre se valida porque se inserta en el SQL.
    /// </summary>
    public static string ForDatabase(string script, string databaseName)
    {
        if (!ValidDatabaseName().IsMatch(databaseName))
        {
            throw new ArgumentException($"Nombre de base de datos no válido: '{databaseName}'.", nameof(databaseName));
        }

        if (databaseName == ScriptDatabaseName)
        {
            return script;
        }

        return script
            .Replace($"N'{ScriptDatabaseName}'", $"N'{databaseName}'", StringComparison.Ordinal)
            .Replace($"CREATE DATABASE {ScriptDatabaseName} ", $"CREATE DATABASE {databaseName} ", StringComparison.Ordinal)
            .Replace($"USE {ScriptDatabaseName};", $"USE {databaseName};", StringComparison.Ordinal);
    }

    /// <summary>Divide el script en lotes por el separador GO, como hace SSMS.</summary>
    public static IEnumerable<string> SplitBatches(string script) =>
        GoSeparator().Split(script).Where(batch => !string.IsNullOrWhiteSpace(batch));

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{0,99}$")]
    private static partial Regex ValidDatabaseName();
}
