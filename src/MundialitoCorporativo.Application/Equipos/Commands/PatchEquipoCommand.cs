using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;

namespace MundialitoCorporativo.Application.Equipos.Commands;

/// <summary>
/// Modificación parcial de un equipo (PATCH): solo se cambian los campos enviados (no nulos).
/// </summary>
public sealed record PatchEquipoCommand(Guid Id, string? Nombre, string? Abreviatura, string? Departamento) : ICommand;

internal sealed class PatchEquipoCommandValidator : AbstractValidator<PatchEquipoCommand>
{
    public PatchEquipoCommandValidator()
    {
        RuleFor(command => command)
            .Must(command => command.Nombre is not null || command.Abreviatura is not null || command.Departamento is not null)
            .WithName("body")
            .WithMessage("Debe enviar al menos un campo para modificar.");

        RuleFor(command => command.Nombre).Length(Equipo.NombreMinLength, Equipo.NombreMaxLength).When(command => command.Nombre is not null);
        RuleFor(command => command.Abreviatura).Length(Equipo.AbreviaturaMinLength, Equipo.AbreviaturaMaxLength).When(command => command.Abreviatura is not null);
        RuleFor(command => command.Departamento).MaximumLength(Equipo.DepartamentoMaxLength);
    }
}

internal sealed class PatchEquipoCommandHandler(IEquipoRepository equipoRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<PatchEquipoCommand>
{
    public async Task<Result> Handle(PatchEquipoCommand command, CancellationToken cancellationToken)
    {
        var equipo = await equipoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (equipo is null)
        {
            return EquipoErrors.NotFound(command.Id);
        }

        var nombre = command.Nombre ?? equipo.Nombre;
        var abreviatura = command.Abreviatura ?? equipo.Abreviatura;
        var departamento = command.Departamento ?? equipo.Departamento;

        var duplicate = await EquipoUniqueness.CheckAsync(equipoRepository, nombre, abreviatura, command.Id, cancellationToken);
        if (duplicate is not null)
        {
            return duplicate;
        }

        var update = equipo.Update(nombre, abreviatura, departamento);
        if (update.IsFailure)
        {
            return update;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
