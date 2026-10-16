using MundialitoCorporativo.Domain.PosicionesJuego;

namespace MundialitoCorporativo.Application.Abstractions.Persistence;

/// <summary>
/// Lectura del catálogo de posiciones de juego para validar jugadores en los Commands.
/// </summary>
public interface IPosicionJuegoRepository
{
    Task<PosicionJuego?> GetByIdAsync(byte id, CancellationToken cancellationToken = default);
}
