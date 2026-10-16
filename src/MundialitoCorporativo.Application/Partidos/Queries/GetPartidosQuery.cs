using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Partidos;

namespace MundialitoCorporativo.Application.Partidos.Queries;

/// <summary>
/// Calendario de partidos con filtros por fecha, equipo y estado (requerimiento ítem 8).
/// </summary>
public sealed record GetPartidosQuery : PagedQuery, IQuery<PagedResponse<PartidoResponse>>
{
    public static readonly IReadOnlyList<string> SortFields = ["fechaProgramada", "estado", "torneo"];

    public Guid? TorneoId { get; init; }

    /// <summary>Fecha inicial (inclusive) del calendario.</summary>
    public DateOnly? FechaDesde { get; init; }

    /// <summary>Fecha final (inclusive) del calendario.</summary>
    public DateOnly? FechaHasta { get; init; }

    /// <summary>Partidos en los que el equipo juega como local o como visitante.</summary>
    public Guid? EquipoId { get; init; }

    public EstadoPartido? Estado { get; init; }

    public override IReadOnlyList<string> AllowedSortFields => SortFields;
}

internal sealed class GetPartidosQueryValidator : PagedQueryValidator<GetPartidosQuery>
{
    public GetPartidosQueryValidator()
    {
        RuleFor(query => query.Estado).IsInEnum();
        RuleFor(query => query.FechaHasta)
            .GreaterThanOrEqualTo(query => query.FechaDesde)
            .When(query => query.FechaDesde is not null && query.FechaHasta is not null)
            .WithMessage("fechaHasta no puede ser anterior a fechaDesde.");
    }
}

internal sealed class GetPartidosQueryHandler(IPartidoQueries partidoQueries)
    : IQueryHandler<GetPartidosQuery, PagedResponse<PartidoResponse>>
{
    public async Task<Result<PagedResponse<PartidoResponse>>> Handle(GetPartidosQuery query, CancellationToken cancellationToken) =>
        await partidoQueries.GetPagedAsync(query, cancellationToken);
}
