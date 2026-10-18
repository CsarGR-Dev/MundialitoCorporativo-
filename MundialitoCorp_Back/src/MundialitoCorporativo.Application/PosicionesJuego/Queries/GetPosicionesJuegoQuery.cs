using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.PosicionesJuego.Queries;

/// <summary>
/// Catálogo de posiciones de juego (Portero, Defensa, Mediocampista, Delantero).
/// </summary>
public sealed record GetPosicionesJuegoQuery : PagedQuery, IQuery<PagedResponse<PosicionJuegoResponse>>
{
    public static readonly IReadOnlyList<string> SortFields = ["orden", "nombre"];

    public bool? Activo { get; init; }

    public override IReadOnlyList<string> AllowedSortFields => SortFields;
}

internal sealed class GetPosicionesJuegoQueryValidator : PagedQueryValidator<GetPosicionesJuegoQuery>;

internal sealed class GetPosicionesJuegoQueryHandler(IPosicionJuegoQueries posicionJuegoQueries)
    : IQueryHandler<GetPosicionesJuegoQuery, PagedResponse<PosicionJuegoResponse>>
{
    public async Task<Result<PagedResponse<PosicionJuegoResponse>>> Handle(GetPosicionesJuegoQuery query, CancellationToken cancellationToken) =>
        await posicionJuegoQueries.GetPagedAsync(query, cancellationToken);
}
