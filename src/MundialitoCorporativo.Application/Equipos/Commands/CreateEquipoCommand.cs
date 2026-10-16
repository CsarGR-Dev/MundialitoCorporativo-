using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;

namespace MundialitoCorporativo.Application.Equipos.Commands;

public sealed record CreateEquipoCommand(string Nombre, string Abreviatura, string? Departamento) : ICommand<Guid>;

internal sealed class CreateEquipoCommandValidator : AbstractValidator<CreateEquipoCommand>
{
    public CreateEquipoCommandValidator()
    {
        RuleFor(command => command.Nombre).NotEmpty().Length(Equipo.NombreMinLength, Equipo.NombreMaxLength);
        RuleFor(command => command.Abreviatura).NotEmpty().Length(Equipo.AbreviaturaMinLength, Equipo.AbreviaturaMaxLength);
        RuleFor(command => command.Departamento).MaximumLength(Equipo.DepartamentoMaxLength);
    }
}

internal sealed class CreateEquipoCommandHandler(IEquipoRepository equipoRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateEquipoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateEquipoCommand command, CancellationToken cancellationToken)
    {
        var duplicate = await EquipoUniqueness.CheckAsync(equipoRepository, command.Nombre, command.Abreviatura, null, cancellationToken);
        if (duplicate is not null)
        {
            return duplicate;
        }

        var equipo = Equipo.Create(command.Nombre, command.Abreviatura, command.Departamento);
        if (equipo.IsFailure)
        {
            return equipo.Error;
        }

        equipoRepository.Add(equipo.Value);

        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess ? equipo.Value.Id : commit.Error;
    }
}
