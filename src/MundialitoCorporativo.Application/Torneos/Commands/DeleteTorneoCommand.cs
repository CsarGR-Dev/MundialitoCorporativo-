using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Torneos.Commands;

public sealed record DeleteTorneoCommand(Guid Id) : ICommand;

internal sealed class DeleteTorneoCommandHandler(
    ITorneoRepository torneoRepository,
    IPartidoRepository partidoRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteTorneoCommand>
{
    public async Task<Result> Handle(DeleteTorneoCommand command, CancellationToken cancellationToken)
    {
        var torneo = await torneoRepository.GetByIdIncludingDeletedAsync(command.Id, cancellationToken);
        if (torneo is null)
        {
            return TorneoErrors.NotFound(command.Id);
        }

        if (torneo.Eliminado)
        {
            return Result.Success();
        }

        if (await partidoRepository.ExistsPlayedInTorneoAsync(command.Id, cancellationToken))
        {
            return TorneoErrors.HasPlayedPartidos;
        }

        torneo.Delete(dateTimeProvider.UtcNow);

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
