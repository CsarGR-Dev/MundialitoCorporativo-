using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Jugadores;
using MundialitoCorporativo.Domain.PosicionesJuego;

namespace MundialitoCorporativo.Application.Jugadores.Commands;

/// <summary>
/// Reglas que requieren consultar la base: posición existente (RN-22) y camiseta única por equipo (RN-02).
/// </summary>
internal static class JugadorRules
{
    public static async Task<Result<PosicionJuego>> GetPosicionJuegoAsync(
        IPosicionJuegoRepository posicionJuegoRepository,
        byte posicionJuegoId,
        CancellationToken cancellationToken)
    {
        var posicionJuego = await posicionJuegoRepository.GetByIdAsync(posicionJuegoId, cancellationToken);

        return posicionJuego is null ? PosicionJuegoErrors.NotFound(posicionJuegoId) : posicionJuego;
    }

    public static async Task<Error?> CheckNumeroCamisetaAsync(
        IJugadorRepository jugadorRepository,
        Guid equipoId,
        int numeroCamiseta,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        await jugadorRepository.ExistsNumeroCamisetaAsync(equipoId, numeroCamiseta, excludeId, cancellationToken)
            ? JugadorErrors.DuplicateNumeroCamiseta(numeroCamiseta)
            : null;
}
