using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Partidos;

namespace MundialitoCorporativo.Application.Partidos.Queries;

/// <summary>
/// Detalle de un partido con su marcador y la lista de goles (goleador, equipo y minuto).
/// </summary>
public sealed record GetPartidoByIdQuery(Guid Id) : IQuery<PartidoDetalleResponse>;

internal sealed class GetPartidoByIdQueryHandler(IPartidoQueries partidoQueries) : IQueryHandler<GetPartidoByIdQuery, PartidoDetalleResponse>
{
    public async Task<Result<PartidoDetalleResponse>> Handle(GetPartidoByIdQuery query, CancellationToken cancellationToken)
    {
        var partido = await partidoQueries.GetByIdAsync(query.Id, cancellationToken);

        return partido is null ? PartidoErrors.NotFound(query.Id) : partido;
    }
}
