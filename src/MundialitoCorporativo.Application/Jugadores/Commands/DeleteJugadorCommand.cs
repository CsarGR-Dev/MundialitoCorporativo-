using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Jugadores;

namespace MundialitoCorporativo.Application.Jugadores.Commands;

/// <summary>
/// Eliminación lógica idempotente de un jugador. Sus goles se conservan en el historial.
/// </summary>
public sealed record DeleteJugadorCommand(Guid Id) : ICommand;

internal sealed class DeleteJugadorCommandHandler(
    IJugadorRepository jugadorRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteJugadorCommand>
{
    public async Task<Result> Handle(DeleteJugadorCommand command, CancellationToken cancellationToken)
    {
        var jugador = await jugadorRepository.GetByIdIncludingDeletedAsync(command.Id, cancellationToken);
        if (jugador is null)
        {
            return JugadorErrors.NotFound(command.Id);
        }

        if (jugador.Eliminado)
        {
            return Result.Success();
        }

        jugador.Delete(dateTimeProvider.UtcNow);

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
