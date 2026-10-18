using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Torneos.Queries;

public sealed record GetGoleadoresQuery : PagedQuery, IQuery<PagedResponse<GoleadorResponse>>
{
    /// <summary>"posicion" ordena por goles (mayor a menor); los empatados comparten posición.</summary>
    public static readonly IReadOnlyList<string> SortFields = ["posicion", "goles", "jugador", "equipo"];

    public Guid TorneoId { get; init; }

    public Guid? EquipoId { get; init; }

    /// <summary>Filtro por nombres o apellidos del jugador (contiene).</summary>
    public string? Jugador { get; init; }

    public override IReadOnlyList<string> AllowedSortFields => SortFields;
}

internal sealed class GetGoleadoresQueryValidator : PagedQueryValidator<GetGoleadoresQuery>
{
    public GetGoleadoresQueryValidator()
    {
        RuleFor(query => query.Jugador).MaximumLength(Domain.Jugadores.Jugador.NombreMaxLength);
    }
}

internal sealed class GetGoleadoresQueryHandler(ITorneoQueries torneoQueries)
    : IQueryHandler<GetGoleadoresQuery, PagedResponse<GoleadorResponse>>
{
    public async Task<Result<PagedResponse<GoleadorResponse>>> Handle(GetGoleadoresQuery query, CancellationToken cancellationToken)
    {
        if (!await torneoQueries.ExistsAsync(query.TorneoId, cancellationToken))
        {
            return TorneoErrors.NotFound(query.TorneoId);
        }

        return await torneoQueries.GetGoleadoresAsync(query, cancellationToken);
    }
}
