using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Torneos.Queries;

public sealed record GetTorneoByIdQuery(Guid Id) : IQuery<TorneoResponse>;

internal sealed class GetTorneoByIdQueryHandler(ITorneoQueries torneoQueries) : IQueryHandler<GetTorneoByIdQuery, TorneoResponse>
{
    public async Task<Result<TorneoResponse>> Handle(GetTorneoByIdQuery query, CancellationToken cancellationToken)
    {
        var torneo = await torneoQueries.GetByIdAsync(query.Id, cancellationToken);

        return torneo is null ? TorneoErrors.NotFound(query.Id) : torneo;
    }
}
