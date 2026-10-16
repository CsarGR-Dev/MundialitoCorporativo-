using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Torneos.Commands;

public sealed record CreateTorneoCommand(string Nombre, string? Descripcion, DateOnly FechaInicio, DateOnly? FechaFin) : ICommand<Guid>;

internal sealed class CreateTorneoCommandValidator : AbstractValidator<CreateTorneoCommand>
{
    public CreateTorneoCommandValidator()
    {
        RuleFor(command => command.Nombre).NotEmpty().Length(Torneo.NombreMinLength, Torneo.NombreMaxLength);
        RuleFor(command => command.Descripcion).MaximumLength(Torneo.DescripcionMaxLength);
        RuleFor(command => command.FechaFin)
            .GreaterThanOrEqualTo(command => command.FechaInicio)
            .When(command => command.FechaFin is not null)
            .WithMessage("La fecha de fin no puede ser anterior a la fecha de inicio.");
    }
}

internal sealed class CreateTorneoCommandHandler(ITorneoRepository torneoRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateTorneoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTorneoCommand command, CancellationToken cancellationToken)
    {
        if (await torneoRepository.ExistsByNombreAsync(command.Nombre, null, cancellationToken))
        {
            return TorneoErrors.DuplicateNombre(command.Nombre.Trim());
        }

        var torneo = Torneo.Create(command.Nombre, command.Descripcion, command.FechaInicio, command.FechaFin);
        if (torneo.IsFailure)
        {
            return torneo.Error;
        }

        torneoRepository.Add(torneo.Value);

        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess ? torneo.Value.Id : commit.Error;
    }
}
