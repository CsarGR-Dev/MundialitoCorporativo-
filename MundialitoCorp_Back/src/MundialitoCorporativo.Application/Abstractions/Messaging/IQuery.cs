using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Abstractions.Messaging;

/// <summary>
/// Query (lado de lectura, requerimiento ítem 3): consulta sin modificar estado, implementada con Dapper.
/// </summary>
public interface IQuery<TResponse>;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
