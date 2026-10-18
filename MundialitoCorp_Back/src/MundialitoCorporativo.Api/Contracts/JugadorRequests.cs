using MundialitoCorporativo.Application.Jugadores.Commands;

namespace MundialitoCorporativo.Api.Contracts;

/// <summary>Cuerpo de PUT de jugadores. El equipo no se cambia; si no se envía Activo, el jugador queda activo.</summary>
public sealed record UpdateJugadorRequest(byte PosicionJuegoId, string Nombres, string Apellidos, int NumeroCamiseta, bool Activo = true)
{
    public UpdateJugadorCommand ToCommand(Guid id) => new(id, PosicionJuegoId, Nombres, Apellidos, NumeroCamiseta, Activo);
}

/// <summary>Cuerpo de PATCH de jugadores: solo se modifican los campos enviados.</summary>
public sealed record PatchJugadorRequest(byte? PosicionJuegoId, string? Nombres, string? Apellidos, int? NumeroCamiseta, bool? Activo)
{
    public PatchJugadorCommand ToCommand(Guid id) => new(id, PosicionJuegoId, Nombres, Apellidos, NumeroCamiseta, Activo);
}
