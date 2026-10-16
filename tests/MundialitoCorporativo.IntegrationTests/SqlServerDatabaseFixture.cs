using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MundialitoCorporativo.Infrastructure.Persistence;

namespace MundialitoCorporativo.IntegrationTests;

/// <summary>
/// Crea una base de datos temporal en SQL Server ejecutando el script de estructura Db/001_DDL_CrearBaseDatos.sql,
/// de modo que las pruebas validan el mapeo de EF Core contra el esquema real. La base se elimina al terminar.
/// </summary>
/// <remarks>
/// Servidor: variable de entorno MUNDIALITO_TEST_SQLSERVER (por defecto la instancia local con autenticación Windows).
/// Si no hay servidor o no se encuentra el script, las pruebas se omiten.
/// </remarks>
public sealed partial class SqlServerDatabaseFixture : IAsyncLifetime
{
    private const string ServerVariable = "MUNDIALITO_TEST_SQLSERVER";
    private const string DefaultServer = "Server=.;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";
    private const string ScriptRelativePath = "Db/001_DDL_CrearBaseDatos.sql";

    private readonly string _databaseName = $"Mundialito_Pruebas_{Guid.NewGuid():N}"[..34];
    private string _serverConnectionString = string.Empty;

    public bool IsAvailable { get; private set; }

    public string UnavailableReason { get; private set; } = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var scriptPath = FindScript();
        if (scriptPath is null)
        {
            UnavailableReason = $"No se encontró el script {ScriptRelativePath}.";
            return;
        }

        _serverConnectionString = Environment.GetEnvironmentVariable(ServerVariable) ?? DefaultServer;

        try
        {
            await using var connection = new SqlConnection(_serverConnectionString);
            await connection.OpenAsync();

            var script = (await File.ReadAllTextAsync(scriptPath))
                .Replace("N'Mundialito'", $"N'{_databaseName}'", StringComparison.Ordinal)
                .Replace("CREATE DATABASE Mundialito ", $"CREATE DATABASE {_databaseName} ", StringComparison.Ordinal)
                .Replace("USE Mundialito;", $"USE {_databaseName};", StringComparison.Ordinal);

            foreach (var batch in GoSeparator().Split(script).Where(batch => !string.IsNullOrWhiteSpace(batch)))
            {
                await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
                await command.ExecuteNonQueryAsync();
            }

            ConnectionString = new SqlConnectionStringBuilder(_serverConnectionString) { InitialCatalog = _databaseName }.ConnectionString;
            IsAvailable = true;
        }
        catch (SqlException exception)
        {
            UnavailableReason = $"SQL Server no disponible: {exception.Message}";
        }
    }

    public async Task DisposeAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(_serverConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}];",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    public MundialitoDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MundialitoDbContext>().UseSqlServer(ConnectionString).Options);

    private static string? FindScript()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, ScriptRelativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerDatabaseFixture>
{
    public const string Name = "SqlServer";
}
