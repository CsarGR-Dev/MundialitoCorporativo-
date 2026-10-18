using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Partidos;

namespace MundialitoCorporativo.Application.Partidos.Commands;

/// <summary>
/// Modificación parcial de un partido (PATCH): cambiar fecha o sede, o cancelarlo con Estado = Cancelado.
/// El resultado no se registra por PATCH sino con <see cref="RegisterPartidoResultCommand"/>.
/// </summary>
public sealed record PatchPartidoCommand(Guid Id, DateTime? FechaProgramada, string? Sede, EstadoPartido? Estado) : ICommand;

internal sealed class PatchPartidoCommandValidator : AbstractValidator<PatchPartidoCommand>
{
    public PatchPartidoCommandValidator()
    {
        RuleFor(command => command)
            .Must(command => command.FechaProgramada is not null || command.Sede is not null || command.Estado is not null)
            .WithName("body")
            .WithMessage("Debe enviar al menos un campo para modificar.");

        RuleFor(command => command.Sede).MaximumLength(Partido.SedeMaxLength);
        RuleFor(command => command.Estado)
            .Must(estado => estado is null or EstadoPartido.Cancelado)
            .WithMessage("Por PATCH solo se permite cambiar el estado a 'Cancelado'. El resultado se registra en /resultado.");
    }
}

internal sealed class PatchPartidoCommandHandler(
    IPartidoRepository partidoRepository,
    ITorneoRepository torneoRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<PatchPartidoCommand>
{
    public async Task<Result> Handle(PatchPartidoCommand command, CancellationToken cancellationToken)
    {
        var partido = await partidoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (partido is null)
        {
            return PartidoErrors.NotFound(command.Id);
        }

        if (command.Estado == EstadoPartido.Cancelado)
        {
            var cancellation = partido.Cancel();
            return cancellation.IsFailure ? cancellation : await unitOfWork.CommitAsync(cancellationToken);
        }

        return await PartidoReschedule.ApplyAsync(
            partido,
            partido.EquipoLocalId,
            partido.EquipoVisitanteId,
            command.FechaProgramada ?? partido.FechaProgramada,
            command.Sede ?? partido.Sede,
            torneoRepository,
            partidoRepository,
            unitOfWork,
            cancellationToken);
    }
}
