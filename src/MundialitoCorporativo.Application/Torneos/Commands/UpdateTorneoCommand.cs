using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Torneos.Commands;

public sealed record UpdateTorneoCommand(Guid Id, string Nombre, string? Descripcion, DateOnly FechaInicio, DateOnly? FechaFin) : ICommand;

internal sealed class UpdateTorneoCommandValidator : AbstractValidator<UpdateTorneoCommand>
{
    public UpdateTorneoCommandValidator()
    {
        RuleFor(command => command.Nombre).NotEmpty().Length(Torneo.NombreMinLength, Torneo.NombreMaxLength);
        RuleFor(command => command.Descripcion).MaximumLength(Torneo.DescripcionMaxLength);
        RuleFor(command => command.FechaFin)
            .GreaterThanOrEqualTo(command => command.FechaInicio)
            .When(command => command.FechaFin is not null)
            .WithMessage("La fecha de fin no puede ser anterior a la fecha de inicio.");
    }
}

internal sealed class UpdateTorneoCommandHandler(ITorneoRepository torneoRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateTorneoCommand>
{
    public async Task<Result> Handle(UpdateTorneoCommand command, CancellationToken cancellationToken)
    {
        var torneo = await torneoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (torneo is null)
        {
            return TorneoErrors.NotFound(command.Id);
        }

        if (await torneoRepository.ExistsByNombreAsync(command.Nombre, command.Id, cancellationToken))
        {
            return TorneoErrors.DuplicateNombre(command.Nombre.Trim());
        }

        var update = torneo.Update(command.Nombre, command.Descripcion, command.FechaInicio, command.FechaFin);
        return update.IsFailure ? update : await unitOfWork.CommitAsync(cancellationToken);
    }
}
