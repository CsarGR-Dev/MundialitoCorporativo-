using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Abstractions.Messaging;

/// <summary>
/// Command (lado de escritura, requerimiento ítem 3): modifica el estado mediante EF Core y el Unit of Work.
/// </summary>
public interface ICommand;

/// <summary>
/// Command que devuelve un valor al completarse (por ejemplo, el Id del recurso creado).
/// </summary>
public interface ICommand<TResponse>;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
