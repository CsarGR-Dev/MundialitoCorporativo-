namespace MundialitoCorporativo.Application.Common.Paging;

/// <summary>
/// Respuesta paginada con el formato del requerimiento (ítem 10):
/// { "data": [], "pageNumber": 1, "pageSize": 10, "totalRecords": 50, "totalPages": 5 }.
/// </summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Data, int PageNumber, int PageSize, int TotalRecords)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);
}
