namespace MundialitoCorporativo.Application.Common.Paging;

public enum SortDirection
{
    Asc = 0,
    Desc = 1
}

/// <summary>
/// Parámetros comunes de todos los GET de colección (requerimiento ítem 10):
/// pageNumber, pageSize, sortBy y sortDirection. Cada query agrega sus filtros por entidad.
/// </summary>
public abstract record PagedQuery
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    public string? SortBy { get; init; }

    public SortDirection SortDirection { get; init; } = SortDirection.Asc;

    /// <summary>Campos por los que se permite ordenar; el primero es el orden por defecto.</summary>
    public abstract IReadOnlyList<string> AllowedSortFields { get; }

    public string EffectiveSortBy => string.IsNullOrWhiteSpace(SortBy) ? AllowedSortFields[0] : SortBy.Trim();

    public int Skip => (PageNumber - 1) * PageSize;
}
