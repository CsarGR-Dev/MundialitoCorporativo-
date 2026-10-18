using System.Data.Common;

namespace MundialitoCorporativo.Application.Abstractions.Data;

/// <summary>
/// Crea conexiones abiertas para las Queries (lado de lectura con Dapper). Nunca se usa en Commands.
/// </summary>
public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
