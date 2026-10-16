using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Jugadores;

namespace MundialitoCorporativo.Application.Jugadores.Commands;

/// <summary>
/// Reemplazo completo de un jugador (PUT). El equipo no se modifica (RN-17).
/// </summary>
public sealed record UpdateJugadorCommand(
    Guid Id,
    byte PosicionJuegoId,
    string Nombres,
    string Apellidos,
    int NumeroCamiseta,
    bool Activo) : ICommand;

internal sealed class UpdateJugadorCommandValidator : AbstractValidator<UpdateJugadorCommand>
{
    public UpdateJugadorCommandValidator()
    {
        RuleFor(command => command.Nombres).NotEmpty().Length(Jugador.NombreMinLength, Jugador.NombreMaxLength);
        RuleFor(command => command.Apellidos).NotEmpty().Length(Jugador.NombreMinLength, Jugador.NombreMaxLength);
        RuleFor(command => command.NumeroCamiseta).InclusiveBetween(Jugador.NumeroCamisetaMin, Jugador.NumeroCamisetaMax);
        RuleFor(command => command.PosicionJuegoId).GreaterThan((byte)0);
    }
}

internal sealed class UpdateJugadorCommandHandler(
    IJugadorRepository jugadorRepository,
    IPosicionJuegoRepository posicionJuegoRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateJugadorCommand>
{
    public async Task<Result> Handle(UpdateJugadorCommand command, CancellationToken cancellationToken)
    {
        var jugador = await jugadorRepository.GetByIdAsync(command.Id, cancellationToken);
        if (jugador is null)
        {
            return JugadorErrors.NotFound(command.Id);
        }

        return await JugadorUpdate.ApplyAsync(
            jugador,
            command.PosicionJuegoId,
            command.Nombres,
            command.Apellidos,
            command.NumeroCamiseta,
            command.Activo,
            jugadorRepository,
            posicionJuegoRepository,
            unitOfWork,
            cancellationToken);
    }
}

/// <summary>
/// Aplica y confirma la actualización de un jugador; compartido por PUT y PATCH.
/// </summary>
internal static class JugadorUpdate
{
    public static async Task<Result> ApplyAsync(
        Jugador jugador,
        byte posicionJuegoId,
        string nombres,
        string apellidos,
        int numeroCamiseta,
        bool activo,
        IJugadorRepository jugadorRepository,
        IPosicionJuegoRepository posicionJuegoRepository,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var posicionJuego = await JugadorRules.GetPosicionJuegoAsync(posicionJuegoRepository, posicionJuegoId, cancellationToken);
        if (posicionJuego.IsFailure)
        {
            return posicionJuego;
        }

        var duplicate = await JugadorRules.CheckNumeroCamisetaAsync(jugadorRepository, jugador.EquipoId, numeroCamiseta, jugador.Id, cancellationToken);
        if (duplicate is not null)
        {
            return duplicate;
        }

        var update = jugador.Update(posicionJuego.Value, nombres, apellidos, numeroCamiseta, activo);
        if (update.IsFailure)
        {
            return update;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
