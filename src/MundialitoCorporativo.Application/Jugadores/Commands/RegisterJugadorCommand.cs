using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Jugadores;

namespace MundialitoCorporativo.Application.Jugadores.Commands;

/// <summary>
/// Registro de un integrante en un equipo (requerimiento ítem 8: "Registro por equipo").
/// </summary>
public sealed record RegisterJugadorCommand(
    Guid EquipoId,
    byte PosicionJuegoId,
    string Nombres,
    string Apellidos,
    int NumeroCamiseta) : ICommand<Guid>;

internal sealed class RegisterJugadorCommandValidator : AbstractValidator<RegisterJugadorCommand>
{
    public RegisterJugadorCommandValidator()
    {
        RuleFor(command => command.Nombres).NotEmpty().Length(Jugador.NombreMinLength, Jugador.NombreMaxLength);
        RuleFor(command => command.Apellidos).NotEmpty().Length(Jugador.NombreMinLength, Jugador.NombreMaxLength);
        RuleFor(command => command.NumeroCamiseta).InclusiveBetween(Jugador.NumeroCamisetaMin, Jugador.NumeroCamisetaMax);
        RuleFor(command => command.PosicionJuegoId).GreaterThan((byte)0);
    }
}

internal sealed class RegisterJugadorCommandHandler(
    IEquipoRepository equipoRepository,
    IJugadorRepository jugadorRepository,
    IPosicionJuegoRepository posicionJuegoRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RegisterJugadorCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterJugadorCommand command, CancellationToken cancellationToken)
    {
        if (await equipoRepository.GetByIdAsync(command.EquipoId, cancellationToken) is null)
        {
            return EquipoErrors.NotFound(command.EquipoId);
        }

        var posicionJuego = await JugadorRules.GetPosicionJuegoAsync(posicionJuegoRepository, command.PosicionJuegoId, cancellationToken);
        if (posicionJuego.IsFailure)
        {
            return posicionJuego.Error;
        }

        var duplicate = await JugadorRules.CheckNumeroCamisetaAsync(jugadorRepository, command.EquipoId, command.NumeroCamiseta, null, cancellationToken);
        if (duplicate is not null)
        {
            return duplicate;
        }

        var jugador = Jugador.Create(command.EquipoId, posicionJuego.Value, command.Nombres, command.Apellidos, command.NumeroCamiseta);
        if (jugador.IsFailure)
        {
            return jugador.Error;
        }

        jugadorRepository.Add(jugador.Value);

        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess ? jugador.Value.Id : commit.Error;
    }
}
