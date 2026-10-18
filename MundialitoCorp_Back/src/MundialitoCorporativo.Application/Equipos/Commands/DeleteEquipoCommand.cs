using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;

namespace MundialitoCorporativo.Application.Equipos.Commands;

/// <summary>
/// Eliminación lógica idempotente: si el equipo ya estaba eliminado devuelve éxito sin cambios;
/// si nunca existió devuelve NotFound (RN-13). No se elimina un equipo con partidos (RN-11).
/// </summary>
public sealed record DeleteEquipoCommand(Guid Id) : ICommand;

internal sealed class DeleteEquipoCommandHandler(
    IEquipoRepository equipoRepository,
    IPartidoRepository partidoRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteEquipoCommand>
{
    public async Task<Result> Handle(DeleteEquipoCommand command, CancellationToken cancellationToken)
    {
        var equipo = await equipoRepository.GetByIdIncludingDeletedAsync(command.Id, cancellationToken);
        if (equipo is null)
        {
            return EquipoErrors.NotFound(command.Id);
        }

        if (equipo.Eliminado)
        {
            return Result.Success();
        }

        if (await partidoRepository.ExistsForEquipoAsync(command.Id, cancellationToken))
        {
            return EquipoErrors.HasPartidos;
        }

        equipo.Delete(dateTimeProvider.UtcNow);

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
