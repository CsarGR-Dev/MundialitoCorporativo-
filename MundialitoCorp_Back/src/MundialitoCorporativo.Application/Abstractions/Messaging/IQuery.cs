using MediatR;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Abstractions.Messaging;

/// <summary>
/// Query (lado de lectura, requerimiento ítem 3): consulta sin modificar estado, implementada con Dapper.
/// Es un request de MediatR que devuelve un <see cref="Result{TValue}"/>.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
