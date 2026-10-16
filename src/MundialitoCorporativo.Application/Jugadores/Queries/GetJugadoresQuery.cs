using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Jugadores;

namespace MundialitoCorporativo.Application.Jugadores.Queries;

/// <summary>
/// Lista paginada de jugadores. Con <see cref="EquipoId"/> devuelve los integrantes de un equipo.
/// </summary>
public sealed record GetJugadoresQuery : PagedQuery, IQuery<PagedResponse<JugadorResponse>>
{
    public static readonly IReadOnlyList<string> SortFields = ["apellidos", "nombres", "numeroCamiseta", "equipo", "posicionJuego"];

    public Guid? EquipoId { get; init; }

    /// <summary>Filtro por nombres o apellidos (contiene).</summary>
    public string? Nombre { get; init; }

    public byte? PosicionJuegoId { get; init; }

    public bool? Activo { get; init; }

    /// <summary>
    /// Indica que la consulta proviene de la ruta del equipo; si el equipo no existe se responde NotFound.
    /// </summary>
    public bool RequireEquipo { get; init; }

    public override IReadOnlyList<string> AllowedSortFields => SortFields;
}

internal sealed class GetJugadoresQueryValidator : PagedQueryValidator<GetJugadoresQuery>
{
    public GetJugadoresQueryValidator()
    {
        RuleFor(query => query.Nombre).MaximumLength(Jugador.NombreMaxLength);
    }
}

internal sealed class GetJugadoresQueryHandler(IJugadorQueries jugadorQueries, IEquipoQueries equipoQueries)
    : IQueryHandler<GetJugadoresQuery, PagedResponse<JugadorResponse>>
{
    public async Task<Result<PagedResponse<JugadorResponse>>> Handle(GetJugadoresQuery query, CancellationToken cancellationToken)
    {
        if (query is { RequireEquipo: true, EquipoId: { } equipoId }
            && !await equipoQueries.ExistsAsync(equipoId, cancellationToken))
        {
            return EquipoErrors.NotFound(equipoId);
        }

        return await jugadorQueries.GetPagedAsync(query, cancellationToken);
    }
}
