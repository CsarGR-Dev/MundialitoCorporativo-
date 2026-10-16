using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Jugadores;

namespace MundialitoCorporativo.Application.Jugadores.Commands;

/// <summary>
/// Modificación parcial de un jugador (PATCH): solo se cambian los campos enviados (no nulos).
/// </summary>
public sealed record PatchJugadorCommand(
    Guid Id,
    byte? PosicionJuegoId,
    string? Nombres,
    string? Apellidos,
    int? NumeroCamiseta,
    bool? Activo) : ICommand;

internal sealed class PatchJugadorCommandValidator : AbstractValidator<PatchJugadorCommand>
{
    public PatchJugadorCommandValidator()
    {
        RuleFor(command => command)
            .Must(command => command.PosicionJuegoId is not null || command.Nombres is not null || command.Apellidos is not null
                             || command.NumeroCamiseta is not null || command.Activo is not null)
            .WithName("body")
            .WithMessage("Debe enviar al menos un campo para modificar.");

        RuleFor(command => command.Nombres).Length(Jugador.NombreMinLength, Jugador.NombreMaxLength).When(command => command.Nombres is not null);
        RuleFor(command => command.Apellidos).Length(Jugador.NombreMinLength, Jugador.NombreMaxLength).When(command => command.Apellidos is not null);
        RuleFor(command => command.NumeroCamiseta).InclusiveBetween(Jugador.NumeroCamisetaMin, Jugador.NumeroCamisetaMax).When(command => command.NumeroCamiseta is not null);
    }
}

internal sealed class PatchJugadorCommandHandler(
    IJugadorRepository jugadorRepository,
    IPosicionJuegoRepository posicionJuegoRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<PatchJugadorCommand>
{
    public async Task<Result> Handle(PatchJugadorCommand command, CancellationToken cancellationToken)
    {
        var jugador = await jugadorRepository.GetByIdAsync(command.Id, cancellationToken);
        if (jugador is null)
        {
            return JugadorErrors.NotFound(command.Id);
        }

        return await JugadorUpdate.ApplyAsync(
            jugador,
            command.PosicionJuegoId ?? jugador.PosicionJuegoId,
            command.Nombres ?? jugador.Nombres,
            command.Apellidos ?? jugador.Apellidos,
            command.NumeroCamiseta ?? jugador.NumeroCamiseta,
            command.Activo ?? jugador.Activo,
            jugadorRepository,
            posicionJuegoRepository,
            unitOfWork,
            cancellationToken);
    }
}
