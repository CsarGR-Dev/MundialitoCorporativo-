using System.Data.Common;
using Microsoft.Data.SqlClient;
using MundialitoCorporativo.Application.Abstractions.Data;

namespace MundialitoCorporativo.Infrastructure.Data;

/// <summary>
/// Crea conexiones de SQL Server para las Queries con Dapper (lado de lectura).
/// </summary>
internal sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
