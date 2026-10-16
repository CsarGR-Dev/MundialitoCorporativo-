using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Torneos.Queries;

public sealed record GetTorneoEquiposQuery : PagedQuery, IQuery<PagedResponse<TorneoEquipoResponse>>
{
    public static readonly IReadOnlyList<string> SortFields = ["equipo", "fechaInscripcion"];

    public Guid TorneoId { get; init; }

    public string? Nombre { get; init; }

    public override IReadOnlyList<string> AllowedSortFields => SortFields;
}

internal sealed class GetTorneoEquiposQueryValidator : PagedQueryValidator<GetTorneoEquiposQuery>
{
    public GetTorneoEquiposQueryValidator()
    {
        RuleFor(query => query.Nombre).MaximumLength(Equipo.NombreMaxLength);
    }
}

internal sealed class GetTorneoEquiposQueryHandler(ITorneoQueries torneoQueries)
    : IQueryHandler<GetTorneoEquiposQuery, PagedResponse<TorneoEquipoResponse>>
{
    public async Task<Result<PagedResponse<TorneoEquipoResponse>>> Handle(GetTorneoEquiposQuery query, CancellationToken cancellationToken)
    {
        if (!await torneoQueries.ExistsAsync(query.TorneoId, cancellationToken))
        {
            return TorneoErrors.NotFound(query.TorneoId);
        }

        return await torneoQueries.GetEquiposPagedAsync(query, cancellationToken);
    }
}
