using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Torneos.Commands;

public sealed record PatchTorneoCommand(
    Guid Id,
    string? Nombre,
    string? Descripcion,
    DateOnly? FechaInicio,
    DateOnly? FechaFin,
    EstadoTorneo? Estado) : ICommand;

internal sealed class PatchTorneoCommandValidator : AbstractValidator<PatchTorneoCommand>
{
    public PatchTorneoCommandValidator()
    {
        RuleFor(command => command)
            .Must(command => command.Nombre is not null || command.Descripcion is not null || command.FechaInicio is not null
                             || command.FechaFin is not null || command.Estado is not null)
            .WithName("body")
            .WithMessage("Debe enviar al menos un campo para modificar.");

        RuleFor(command => command.Nombre).Length(Torneo.NombreMinLength, Torneo.NombreMaxLength).When(command => command.Nombre is not null);
        RuleFor(command => command.Descripcion).MaximumLength(Torneo.DescripcionMaxLength);
        RuleFor(command => command.Estado).IsInEnum();
    }
}

internal sealed class PatchTorneoCommandHandler(ITorneoRepository torneoRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<PatchTorneoCommand>
{
    public async Task<Result> Handle(PatchTorneoCommand command, CancellationToken cancellationToken)
    {
        var torneo = await torneoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (torneo is null)
        {
            return TorneoErrors.NotFound(command.Id);
        }

        var changesData = command.Nombre is not null || command.Descripcion is not null || command.FechaInicio is not null || command.FechaFin is not null;
        if (changesData)
        {
            var nombre = command.Nombre ?? torneo.Nombre;
            if (await torneoRepository.ExistsByNombreAsync(nombre, command.Id, cancellationToken))
            {
                return TorneoErrors.DuplicateNombre(nombre.Trim());
            }

            var update = torneo.Update(
                nombre,
                command.Descripcion ?? torneo.Descripcion,
                command.FechaInicio ?? torneo.FechaInicio,
                command.FechaFin ?? torneo.FechaFin);

            if (update.IsFailure)
            {
                return update;
            }
        }

        if (command.Estado is { } estado)
        {
            var statusChange = torneo.ChangeStatus(estado);
            if (statusChange.IsFailure)
            {
                return statusChange;
            }
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
