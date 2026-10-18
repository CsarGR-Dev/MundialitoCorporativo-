using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Partidos;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Partidos.Commands;

/// <summary>
/// Programa un partido en el calendario de un torneo (requerimiento ítem 8: "Creación").
/// </summary>
public sealed record SchedulePartidoCommand(
    Guid TorneoId,
    Guid EquipoLocalId,
    Guid EquipoVisitanteId,
    DateTime FechaProgramada,
    string? Sede) : ICommand<Guid>;

internal sealed class SchedulePartidoCommandValidator : AbstractValidator<SchedulePartidoCommand>
{
    public SchedulePartidoCommandValidator()
    {
        RuleFor(command => command.TorneoId).NotEmpty();
        RuleFor(command => command.EquipoLocalId).NotEmpty();
        RuleFor(command => command.EquipoVisitanteId)
            .NotEmpty()
            .NotEqual(command => command.EquipoLocalId)
            .WithMessage("El equipo local y el visitante deben ser distintos.");
        RuleFor(command => command.FechaProgramada).NotEmpty();
        RuleFor(command => command.Sede).MaximumLength(Partido.SedeMaxLength);
    }
}

internal sealed class SchedulePartidoCommandHandler(
    ITorneoRepository torneoRepository,
    IPartidoRepository partidoRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SchedulePartidoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SchedulePartidoCommand command, CancellationToken cancellationToken)
    {
        var torneo = await torneoRepository.GetByIdAsync(command.TorneoId, cancellationToken);
        if (torneo is null)
        {
            return TorneoErrors.NotFound(command.TorneoId);
        }

        var partido = Partido.Schedule(torneo, command.EquipoLocalId, command.EquipoVisitanteId, command.FechaProgramada, command.Sede);
        if (partido.IsFailure)
        {
            return partido.Error;
        }

        var unavailable = await PartidoScheduleRules.CheckAvailabilityAsync(
            partidoRepository, command.EquipoLocalId, command.EquipoVisitanteId, partido.Value.FechaProgramada, null, cancellationToken);
        if (unavailable is not null)
        {
            return unavailable;
        }

        partidoRepository.Add(partido.Value);

        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess ? partido.Value.Id : commit.Error;
    }
}
