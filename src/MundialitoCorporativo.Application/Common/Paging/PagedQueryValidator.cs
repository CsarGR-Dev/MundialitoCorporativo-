using FluentValidation;

namespace MundialitoCorporativo.Application.Common.Paging;

/// <summary>
/// Reglas de paginación y ordenamiento comunes: página ≥ 1, tamaño entre 1 y 100 y
/// sortBy restringido a una lista blanca (evita inyección SQL en el ORDER BY).
/// </summary>
public abstract class PagedQueryValidator<TQuery> : AbstractValidator<TQuery>
    where TQuery : PagedQuery
{
    protected PagedQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("pageNumber debe ser mayor o igual a 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PagedQuery.MaxPageSize)
            .WithMessage($"pageSize debe estar entre 1 y {PagedQuery.MaxPageSize}.");

        RuleFor(query => query.SortBy)
            .Must((query, sortBy) => sortBy is null
                                     || query.AllowedSortFields.Contains(sortBy.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage(query => $"sortBy no es válido. Valores permitidos: {string.Join(", ", query.AllowedSortFields)}.");

        RuleFor(query => query.SortDirection)
            .IsInEnum()
            .WithMessage("sortDirection debe ser 'asc' o 'desc'.");
    }
}
