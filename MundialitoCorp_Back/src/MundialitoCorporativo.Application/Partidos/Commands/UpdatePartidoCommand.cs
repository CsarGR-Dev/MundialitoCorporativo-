using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Partidos;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Partidos.Commands;

/// <summary>
/// Reprogramación completa de un partido (PUT). Solo para partidos en estado Programado; el torneo no cambia.
/// </summary>
public sealed record UpdatePartidoCommand(
    Guid Id,
    Guid EquipoLocalId,
    Guid EquipoVisitanteId,
    DateTime FechaProgramada,
    string? Sede) : ICommand;

internal sealed class UpdatePartidoCommandValidator : AbstractValidator<UpdatePartidoCommand>
{
    public UpdatePartidoCommandValidator()
    {
        RuleFor(command => command.EquipoLocalId).NotEmpty();
        RuleFor(command => command.EquipoVisitanteId)
            .NotEmpty()
            .NotEqual(command => command.EquipoLocalId)
            .WithMessage("El equipo local y el visitante deben ser distintos.");
        RuleFor(command => command.FechaProgramada).NotEmpty();
        RuleFor(command => command.Sede).MaximumLength(Partido.SedeMaxLength);
    }
}

internal sealed class UpdatePartidoCommandHandler(
    IPartidoRepository partidoRepository,
    ITorneoRepository torneoRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdatePartidoCommand>
{
    public async Task<Result> Handle(UpdatePartidoCommand command, CancellationToken cancellationToken)
    {
        var partido = await partidoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (partido is null)
        {
            return PartidoErrors.NotFound(command.Id);
        }

        return await PartidoReschedule.ApplyAsync(
            partido, command.EquipoLocalId, command.EquipoVisitanteId, command.FechaProgramada, command.Sede,
            torneoRepository, partidoRepository, unitOfWork, cancellationToken);
    }
}

/// <summary>
/// Aplica y confirma la reprogramación de un partido; compartido por PUT y PATCH.
/// </summary>
internal static class PartidoReschedule
{
    public static async Task<Result> ApplyAsync(
        Partido partido,
        Guid equipoLocalId,
        Guid equipoVisitanteId,
        DateTime fechaProgramada,
        string? sede,
        ITorneoRepository torneoRepository,
        IPartidoRepository partidoRepository,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var torneo = await torneoRepository.GetByIdAsync(partido.TorneoId, cancellationToken);
        if (torneo is null)
        {
            return TorneoErrors.NotFound(partido.TorneoId);
        }

        var update = partido.Update(torneo, equipoLocalId, equipoVisitanteId, fechaProgramada, sede);
        if (update.IsFailure)
        {
            return update;
        }

        var unavailable = await PartidoScheduleRules.CheckAvailabilityAsync(
            partidoRepository, equipoLocalId, equipoVisitanteId, partido.FechaProgramada, partido.Id, cancellationToken);
        if (unavailable is not null)
        {
            return unavailable;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
