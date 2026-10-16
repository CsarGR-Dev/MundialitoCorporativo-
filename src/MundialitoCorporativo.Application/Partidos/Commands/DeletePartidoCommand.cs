using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Partidos;

namespace MundialitoCorporativo.Application.Partidos.Commands;

/// <summary>
/// Eliminación lógica idempotente de un partido. Un partido jugado no puede eliminarse (RN-12).
/// </summary>
public sealed record DeletePartidoCommand(Guid Id) : ICommand;

internal sealed class DeletePartidoCommandHandler(
    IPartidoRepository partidoRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeletePartidoCommand>
{
    public async Task<Result> Handle(DeletePartidoCommand command, CancellationToken cancellationToken)
    {
        var partido = await partidoRepository.GetByIdIncludingDeletedAsync(command.Id, cancellationToken);
        if (partido is null)
        {
            return PartidoErrors.NotFound(command.Id);
        }

        if (partido.Eliminado)
        {
            return Result.Success();
        }

        var deletion = partido.Delete(dateTimeProvider.UtcNow);
        return deletion.IsFailure ? deletion : await unitOfWork.CommitAsync(cancellationToken);
    }
}
