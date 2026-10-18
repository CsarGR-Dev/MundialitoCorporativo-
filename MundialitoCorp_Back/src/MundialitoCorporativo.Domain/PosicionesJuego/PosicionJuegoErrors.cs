using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Domain.PosicionesJuego;

public static class PosicionJuegoErrors
{
    public static Error NotFound(byte id) =>
        Error.Validation($"La posición de juego '{id}' no existe.", "PosicionJuego.NotFound");

    public static Error Inactive(byte id) =>
        Error.Validation($"La posición de juego '{id}' está inactiva.", "PosicionJuego.Inactive");
}
