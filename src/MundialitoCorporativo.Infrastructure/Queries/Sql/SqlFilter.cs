using Dapper;

namespace MundialitoCorporativo.Infrastructure.Queries.Sql;

/// <summary>
/// Construye la cláusula WHERE solo con los filtros enviados. Todos los valores van como parámetros
/// (nunca concatenados), y una consulta sin filtros opcionales no arrastra condiciones "@x IS NULL OR ...",
/// lo que permite a SQL Server usar los índices.
/// </summary>
internal sealed class SqlFilter
{
    private readonly List<string> _conditions = [];

    public SqlFilter(params string[] mandatoryConditions)
    {
        _conditions.AddRange(mandatoryConditions);
    }

    public DynamicParameters Parameters { get; } = new();

    public SqlFilter Add(string condition, string parameterName, object value)
    {
        _conditions.Add(condition);
        Parameters.Add(parameterName, value);
        return this;
    }

    public SqlFilter AddIf(bool apply, string condition, string parameterName, Func<object> value) =>
        apply ? Add(condition, parameterName, value()) : this;

    /// <summary>Filtro "contiene" seguro: escapa los comodines de LIKE del texto recibido.</summary>
    public SqlFilter AddContains(string? text, string condition, string parameterName) =>
        string.IsNullOrWhiteSpace(text) ? this : Add(condition, parameterName, $"%{EscapeLike(text.Trim())}%");

    public string ToWhereClause() => _conditions.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", _conditions);

    private static string EscapeLike(string text) =>
        text.Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
}
