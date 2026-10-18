using MundialitoCorporativo.Application.Common.Paging;

namespace MundialitoCorporativo.Infrastructure.Queries.Sql;

/// <summary>
/// Ordenamiento dinámico seguro: el sortBy recibido (ya validado contra la lista blanca de la query)
/// se traduce a una columna conocida; el texto del usuario nunca llega al SQL.
/// </summary>
internal sealed class SqlSort(IReadOnlyDictionary<string, string> columns, string tieBreaker)
{
    public IReadOnlyDictionary<string, string> Columns { get; } = columns;

    public string ToOrderBy(PagedQuery query)
    {
        if (!Columns.TryGetValue(query.EffectiveSortBy, out var column))
        {
            throw new InvalidOperationException($"No hay columna configurada para ordenar por '{query.EffectiveSortBy}'.");
        }

        var direction = query.SortDirection == SortDirection.Desc ? "DESC" : "ASC";

        // Las columnas de desempate garantizan un orden estable entre páginas. SQL Server no admite repetir
        // una columna en el ORDER BY, por eso se omiten las que ya son la columna de orden.
        var tieBreakers = tieBreaker
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(tieColumn => !string.Equals(tieColumn, column, StringComparison.OrdinalIgnoreCase));

        return string.Join(", ", new[] { $"{column} {direction}" }.Concat(tieBreakers));
    }

    public static SqlSort For(string tieBreaker, params (string Field, string Column)[] columns) =>
        new(columns.ToDictionary(item => item.Field, item => item.Column, StringComparer.OrdinalIgnoreCase), tieBreaker);
}
