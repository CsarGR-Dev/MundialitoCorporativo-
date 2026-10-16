using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;

namespace MundialitoCorporativo.Application.Equipos.Queries;

/// <summary>
/// Lista paginada de equipos con filtros y ordenamiento dinámico (requerimiento ítem 8).
/// </summary>
public sealed record GetEquiposQuery : PagedQuery, IQuery<PagedResponse<EquipoResponse>>
{
    public static readonly IReadOnlyList<string> SortFields = ["nombre", "abreviatura", "departamento", "fechaCreacion"];

    /// <summary>Filtro por nombre o abreviatura (contiene).</summary>
    public string? Nombre { get; init; }

    public string? Departamento { get; init; }

    public override IReadOnlyList<string> AllowedSortFields => SortFields;
}

internal sealed class GetEquiposQueryValidator : PagedQueryValidator<GetEquiposQuery>
{
    public GetEquiposQueryValidator()
    {
        RuleFor(query => query.Nombre).MaximumLength(Equipo.NombreMaxLength);
        RuleFor(query => query.Departamento).MaximumLength(Equipo.DepartamentoMaxLength);
    }
}

internal sealed class GetEquiposQueryHandler(IEquipoQueries equipoQueries)
    : IQueryHandler<GetEquiposQuery, PagedResponse<EquipoResponse>>
{
    public async Task<Result<PagedResponse<EquipoResponse>>> Handle(GetEquiposQuery query, CancellationToken cancellationToken) =>
        await equipoQueries.GetPagedAsync(query, cancellationToken);
}
