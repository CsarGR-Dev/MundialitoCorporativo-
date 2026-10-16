using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;

namespace MundialitoCorporativo.Application.Equipos.Queries;

public sealed record GetEquipoByIdQuery(Guid Id) : IQuery<EquipoResponse>;

internal sealed class GetEquipoByIdQueryHandler(IEquipoQueries equipoQueries) : IQueryHandler<GetEquipoByIdQuery, EquipoResponse>
{
    public async Task<Result<EquipoResponse>> Handle(GetEquipoByIdQuery query, CancellationToken cancellationToken)
    {
        var equipo = await equipoQueries.GetByIdAsync(query.Id, cancellationToken);

        return equipo is null ? EquipoErrors.NotFound(query.Id) : equipo;
    }
}
