using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Torneos.Commands;

public sealed record RegisterEquipoInTorneoCommand(Guid TorneoId, Guid EquipoId) : ICommand;

internal sealed class RegisterEquipoInTorneoCommandHandler(
    ITorneoRepository torneoRepository,
    IEquipoRepository equipoRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RegisterEquipoInTorneoCommand>
{
    public async Task<Result> Handle(RegisterEquipoInTorneoCommand command, CancellationToken cancellationToken)
    {
        var torneo = await torneoRepository.GetByIdAsync(command.TorneoId, cancellationToken);
        if (torneo is null)
        {
            return TorneoErrors.NotFound(command.TorneoId);
        }

        if (await equipoRepository.GetByIdAsync(command.EquipoId, cancellationToken) is null)
        {
            return EquipoErrors.NotFound(command.EquipoId);
        }

        var registration = torneo.RegisterEquipo(command.EquipoId, dateTimeProvider.UtcNow);
        return registration.IsFailure ? registration : await unitOfWork.CommitAsync(cancellationToken);
    }
}
