using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Torneos.Queries;

public sealed record GetTablaPosicionesQuery : PagedQuery, IQuery<PagedResponse<FilaTablaPosicionesResponse>>
{
    /// <summary>"posicion" aplica el orden oficial del torneo; el resto reordena sin alterar la posición calculada.</summary>
    public static readonly IReadOnlyList<string> SortFields = ["posicion", "equipo", "puntos", "diferenciaGol", "golesFavor"];

    public Guid TorneoId { get; init; }

    /// <summary>Filtro por nombre o abreviatura del equipo (contiene).</summary>
    public string? Equipo { get; init; }

    public override IReadOnlyList<string> AllowedSortFields => SortFields;
}

internal sealed class GetTablaPosicionesQueryValidator : PagedQueryValidator<GetTablaPosicionesQuery>
{
    public GetTablaPosicionesQueryValidator()
    {
        RuleFor(query => query.Equipo).MaximumLength(Domain.Equipos.Equipo.NombreMaxLength);
    }
}

internal sealed class GetTablaPosicionesQueryHandler(ITorneoQueries torneoQueries)
    : IQueryHandler<GetTablaPosicionesQuery, PagedResponse<FilaTablaPosicionesResponse>>
{
    public async Task<Result<PagedResponse<FilaTablaPosicionesResponse>>> Handle(GetTablaPosicionesQuery query, CancellationToken cancellationToken)
    {
        if (!await torneoQueries.ExistsAsync(query.TorneoId, cancellationToken))
        {
            return TorneoErrors.NotFound(query.TorneoId);
        }

        return await torneoQueries.GetTablaPosicionesAsync(query, cancellationToken);
    }
}
