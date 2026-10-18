using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MundialitoCorporativo.Infrastructure.Initialization;
using MundialitoCorporativo.Infrastructure.Persistence;

namespace MundialitoCorporativo.IntegrationTests;

/// <summary>
/// Crea una base de datos temporal en SQL Server ejecutando el script de estructura 001_DDL_CrearBaseDatos.sql
/// (el mismo que usa la API al iniciar), de modo que las pruebas validan el mapeo de EF Core contra el esquema real.
/// La base se elimina al terminar.
/// </summary>
/// <remarks>
/// Servidor: variable de entorno MUNDIALITO_TEST_SQLSERVER (por defecto la instancia local con autenticación Windows).
/// Si no hay servidor, las pruebas se omiten.
/// </remarks>
public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    private const string ServerVariable = "MUNDIALITO_TEST_SQLSERVER";
    private const string DefaultServer = "Server=.;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";
    private const string ScriptFolder = "Db";

    private readonly string _databaseName = $"Mundialito_Pruebas_{Guid.NewGuid():N}"[..34];

    public bool IsAvailable { get; private set; }

    public string UnavailableReason { get; private set; } = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Conexión al servidor (base master), para pruebas que crean sus propias bases.</summary>
    public string ServerConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        ServerConnectionString = Environment.GetEnvironmentVariable(ServerVariable) ?? DefaultServer;

        try
        {
            await RunScriptAsync(ServerConnectionString, SqlScripts.Load(SqlScripts.CrearBaseDatos), _databaseName);

            ConnectionString = new SqlConnectionStringBuilder(ServerConnectionString) { InitialCatalog = _databaseName }.ConnectionString;
            IsAvailable = true;
        }
        catch (SqlException exception)
        {
            UnavailableReason = $"SQL Server no disponible: {exception.Message}";
        }
    }

    public async Task DisposeAsync()
    {
        if (IsAvailable)
        {
            await DropDatabaseAsync(ServerConnectionString, _databaseName);
        }
    }

    public MundialitoDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MundialitoDbContext>().UseSqlServer(ConnectionString).Options);

    /// <summary>Ejecuta un script SQL por lotes (separador GO) sobre la base indicada.</summary>
    public static async Task RunScriptAsync(string serverConnectionString, string script, string databaseName)
    {
        await using var connection = new SqlConnection(serverConnectionString);
        await connection.OpenAsync();

        foreach (var batch in SqlScripts.SplitBatches(SqlScripts.ForDatabase(script, databaseName)))
        {
            await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
            await command.ExecuteNonQueryAsync();
        }
    }

    public static async Task DropDatabaseAsync(string serverConnectionString, string databaseName)
    {
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(serverConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            $"IF DB_ID(N'{databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Busca un script de la carpeta local Db/ (scripts de trabajo para SSMS, fuera del repositorio).
    /// Devuelve null si la carpeta no existe, por ejemplo en un clon del repositorio.
    /// </summary>
    public static string? FindDbScript(string fileName)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            // Separadores del sistema: con rutas largas (prefijo \\?\) Windows no normaliza "/".
            var candidate = Path.Combine(directory.FullName, ScriptFolder, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerDatabaseFixture>
{
    public const string Name = "SqlServer";
}
