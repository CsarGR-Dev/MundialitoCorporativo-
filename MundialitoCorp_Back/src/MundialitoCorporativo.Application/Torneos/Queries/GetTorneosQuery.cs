using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Torneos.Queries;

public sealed record GetTorneosQuery : PagedQuery, IQuery<PagedResponse<TorneoResponse>>
{
    public static readonly IReadOnlyList<string> SortFields = ["fechaInicio", "nombre", "estado"];

    public string? Nombre { get; init; }

    public EstadoTorneo? Estado { get; init; }

    public override IReadOnlyList<string> AllowedSortFields => SortFields;
}

internal sealed class GetTorneosQueryValidator : PagedQueryValidator<GetTorneosQuery>
{
    public GetTorneosQueryValidator()
    {
        RuleFor(query => query.Nombre).MaximumLength(Torneo.NombreMaxLength);
        RuleFor(query => query.Estado).IsInEnum();
    }
}

internal sealed class GetTorneosQueryHandler(ITorneoQueries torneoQueries)
    : IQueryHandler<GetTorneosQuery, PagedResponse<TorneoResponse>>
{
    public async Task<Result<PagedResponse<TorneoResponse>>> Handle(GetTorneosQuery query, CancellationToken cancellationToken) =>
        await torneoQueries.GetPagedAsync(query, cancellationToken);
}
