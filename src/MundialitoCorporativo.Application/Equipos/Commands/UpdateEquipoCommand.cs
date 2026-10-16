using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;

namespace MundialitoCorporativo.Application.Equipos.Commands;

/// <summary>
/// Reemplazo completo de un equipo (PUT): todos los campos se envían; no crea el recurso si no existe.
/// </summary>
public sealed record UpdateEquipoCommand(Guid Id, string Nombre, string Abreviatura, string? Departamento) : ICommand;

internal sealed class UpdateEquipoCommandValidator : AbstractValidator<UpdateEquipoCommand>
{
    public UpdateEquipoCommandValidator()
    {
        RuleFor(command => command.Nombre).NotEmpty().Length(Equipo.NombreMinLength, Equipo.NombreMaxLength);
        RuleFor(command => command.Abreviatura).NotEmpty().Length(Equipo.AbreviaturaMinLength, Equipo.AbreviaturaMaxLength);
        RuleFor(command => command.Departamento).MaximumLength(Equipo.DepartamentoMaxLength);
    }
}

internal sealed class UpdateEquipoCommandHandler(IEquipoRepository equipoRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateEquipoCommand>
{
    public async Task<Result> Handle(UpdateEquipoCommand command, CancellationToken cancellationToken)
    {
        var equipo = await equipoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (equipo is null)
        {
            return EquipoErrors.NotFound(command.Id);
        }

        var duplicate = await EquipoUniqueness.CheckAsync(equipoRepository, command.Nombre, command.Abreviatura, command.Id, cancellationToken);
        if (duplicate is not null)
        {
            return duplicate;
        }

        var update = equipo.Update(command.Nombre, command.Abreviatura, command.Departamento);
        if (update.IsFailure)
        {
            return update;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
