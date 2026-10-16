using Dapper;
using MundialitoCorporativo.Application.Abstractions.Data;
using MundialitoCorporativo.Application.Common.Paging;

namespace MundialitoCorporativo.Infrastructure.Queries.Sql;

/// <summary>
/// Paginación en la base de datos (requerimiento ítem 10: no se trae todo a memoria). En un solo viaje
/// ejecuta el conteo total y la página solicitada con OFFSET/FETCH.
/// </summary>
internal static class PagedSql
{
    public static async Task<PagedResponse<T>> QueryAsync<T>(
        IDbConnectionFactory connectionFactory,
        PagedQuery query,
        string select,
        string from,
        SqlFilter filter,
        SqlSort sort,
        CancellationToken cancellationToken,
        string? commonTableExpressions = null)
    {
        var where = filter.ToWhereClause();
        var cte = commonTableExpressions ?? string.Empty;

        var sql = $"""
            {cte}
            SELECT COUNT(*) {from} {where};
            {cte}
            SELECT {select} {from} {where}
            ORDER BY {sort.ToOrderBy(query)}
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        filter.Parameters.Add("Skip", query.Skip);
        filter.Parameters.Add("Take", query.PageSize);

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, filter.Parameters, cancellationToken: cancellationToken));

        var totalRecords = await results.ReadSingleAsync<int>();
        var data = (await results.ReadAsync<T>()).AsList();

        return new PagedResponse<T>(data, query.PageNumber, query.PageSize, totalRecords);
    }

    public static async Task<bool> ExistsAsync(
        IDbConnectionFactory connectionFactory,
        string table,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {table} WHERE Id = @Id AND Eliminado = 0) THEN 1 ELSE 0 END",
            new { Id = id },
            cancellationToken: cancellationToken));
    }
}
