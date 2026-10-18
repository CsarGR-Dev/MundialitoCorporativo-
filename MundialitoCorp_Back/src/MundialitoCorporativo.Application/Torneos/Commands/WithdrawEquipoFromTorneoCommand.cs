using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Torneos.Commands;

public sealed record WithdrawEquipoFromTorneoCommand(Guid TorneoId, Guid EquipoId) : ICommand;

internal sealed class WithdrawEquipoFromTorneoCommandHandler(
    ITorneoRepository torneoRepository,
    IPartidoRepository partidoRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<WithdrawEquipoFromTorneoCommand>
{
    public async Task<Result> Handle(WithdrawEquipoFromTorneoCommand command, CancellationToken cancellationToken)
    {
        var torneo = await torneoRepository.GetByIdAsync(command.TorneoId, cancellationToken);
        if (torneo is null)
        {
            return TorneoErrors.NotFound(command.TorneoId);
        }

        // DELETE de una inscripción inexistente: 404 (el recurso no existe; el estado final es el mismo).
        if (!torneo.IsEquipoRegistered(command.EquipoId))
        {
            return TorneoErrors.InscripcionNotFound(command.EquipoId);
        }

        if (await partidoRepository.ExistsForEquipoInTorneoAsync(command.TorneoId, command.EquipoId, cancellationToken))
        {
            return TorneoErrors.EquipoHasPartidos;
        }

        var withdrawal = torneo.WithdrawEquipo(command.EquipoId);
        return withdrawal.IsFailure ? withdrawal : await unitOfWork.CommitAsync(cancellationToken);
    }
}
