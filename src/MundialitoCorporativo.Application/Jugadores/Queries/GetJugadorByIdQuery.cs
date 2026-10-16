using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Jugadores;

namespace MundialitoCorporativo.Application.Jugadores.Queries;

public sealed record GetJugadorByIdQuery(Guid Id) : IQuery<JugadorResponse>;

internal sealed class GetJugadorByIdQueryHandler(IJugadorQueries jugadorQueries) : IQueryHandler<GetJugadorByIdQuery, JugadorResponse>
{
    public async Task<Result<JugadorResponse>> Handle(GetJugadorByIdQuery query, CancellationToken cancellationToken)
    {
        var jugador = await jugadorQueries.GetByIdAsync(query.Id, cancellationToken);

        return jugador is null ? JugadorErrors.NotFound(query.Id) : jugador;
    }
}
