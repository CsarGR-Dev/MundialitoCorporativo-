using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Domain.Jugadores;

public static class JugadorErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound($"No existe el jugador '{id}'.", "Jugador.NotFound");

    public static Error DuplicateNumeroCamiseta(int numeroCamiseta) =>
        Error.Conflict($"El número de camiseta {numeroCamiseta} ya está asignado en el equipo.", "Jugador.DuplicateNumeroCamiseta");

    public static readonly Error InvalidNombres =
        Error.Validation("Los nombres son obligatorios y deben tener entre 2 y 60 caracteres.", "Jugador.InvalidNombres");

    public static readonly Error InvalidApellidos =
        Error.Validation("Los apellidos son obligatorios y deben tener entre 2 y 60 caracteres.", "Jugador.InvalidApellidos");

    public static readonly Error InvalidNumeroCamiseta =
        Error.Validation("El número de camiseta debe estar entre 1 y 99.", "Jugador.InvalidNumeroCamiseta");

    public static readonly Error EquipoChangeNotAllowed =
        Error.Validation("Un jugador no puede cambiar de equipo.", "Jugador.EquipoChangeNotAllowed");
}
