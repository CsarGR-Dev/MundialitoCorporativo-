using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MundialitoCorporativo.Infrastructure.Seed;

namespace MundialitoCorporativo.Infrastructure.Initialization;

/// <summary>
/// Deja la base lista al iniciar la API: espera a SQL Server, aplica el esquema de Db/ y ejecuta el seed
/// si la base está vacía (requerimiento ítem 9). Con Docker basta con <c>docker-compose up</c>.
/// </summary>
internal sealed partial class DatabaseInitializer(
    string connectionString,
    IOptions<DatabaseOptions> options,
    DatabaseSeeder seeder,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (settings.ApplySchemaOnStartup)
        {
            await ApplySchemaAsync(settings, cancellationToken);
        }

        if (settings.SeedOnStartup)
        {
            await seeder.SeedIfEmptyAsync(cancellationToken);
        }
    }

    private async Task ApplySchemaAsync(DatabaseOptions settings, CancellationToken cancellationToken)
    {
        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;

        // Los scripts crean la base, así que se ejecutan conectados a master.
        var masterConnectionString = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" }.ConnectionString;
        await using var connection = await OpenWithRetriesAsync(masterConnectionString, settings, cancellationToken);

        foreach (var fileName in SqlScripts.Schema)
        {
            var script = SqlScripts.ForDatabase(SqlScripts.Load(fileName), databaseName);
            foreach (var batch in SqlScripts.SplitBatches(script))
            {
                await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        LogSchemaApplied(logger, databaseName);
    }

    private async Task<SqlConnection> OpenWithRetriesAsync(string masterConnectionString, DatabaseOptions settings, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var connection = new SqlConnection(masterConnectionString);
            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch (SqlException exception) when (attempt < settings.ConnectionRetries)
            {
                await connection.DisposeAsync();
                LogConnectionRetry(logger, attempt, settings.ConnectionRetries, settings.ConnectionRetryDelay.TotalSeconds, exception.Number);
                await Task.Delay(settings.ConnectionRetryDelay, cancellationToken);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Esquema de la base {DatabaseName} verificado con los scripts de Db/")]
    private static partial void LogSchemaApplied(ILogger logger, string databaseName);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "SQL Server no disponible (intento {Attempt} de {MaxAttempts}, error SQL {SqlErrorNumber}); nuevo intento en {DelaySeconds} s")]
    private static partial void LogConnectionRetry(ILogger logger, int attempt, int maxAttempts, double delaySeconds, int sqlErrorNumber);
}
