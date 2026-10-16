using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Abstractions.Messaging;

/// <summary>
/// Punto de entrada único para ejecutar Commands y Queries. Valida la entrada antes de invocar el handler.
/// </summary>
public interface IDispatcher
{
    Task<Result> Send(ICommand command, CancellationToken cancellationToken = default);

    Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    Task<Result<TResponse>> Query<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
