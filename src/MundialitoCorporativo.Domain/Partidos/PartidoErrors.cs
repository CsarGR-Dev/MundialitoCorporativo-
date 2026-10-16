using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Domain.Partidos;

public static class PartidoErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound($"No existe el partido '{id}'.", "Partido.NotFound");

    public static readonly Error SameEquipo =
        Error.Validation("El equipo local y el visitante deben ser distintos.", "Partido.SameEquipo");

    public static readonly Error SedeTooLong =
        Error.Validation("La sede no puede superar 100 caracteres.", "Partido.SedeTooLong");

    public static readonly Error EquipoAlreadyHasPartidoAtTime =
        Error.Conflict("Uno de los equipos ya tiene un partido programado en esa fecha y hora.", "Partido.EquipoAlreadyHasPartidoAtTime");

    public static readonly Error NotScheduled =
        Error.Conflict("Solo se puede modificar un partido en estado programado.", "Partido.NotScheduled");

    public static readonly Error AlreadyPlayed =
        Error.Conflict("El partido ya tiene un resultado registrado.", "Partido.AlreadyPlayed");

    public static readonly Error IsCancelled =
        Error.Conflict("El partido está cancelado.", "Partido.IsCancelled");

    public static readonly Error PlayedCannotBeDeleted =
        Error.Conflict("Un partido jugado no puede eliminarse.", "Partido.PlayedCannotBeDeleted");

    public static readonly Error PlayedCannotBeCancelled =
        Error.Conflict("Un partido jugado no puede cancelarse.", "Partido.PlayedCannotBeCancelled");

    public static readonly Error InvalidScore =
        Error.Validation("Los goles de cada equipo deben estar entre 0 y 99.", "Partido.InvalidScore");

    public static readonly Error GolesMismatch =
        Error.Validation("La cantidad de goles registrados por equipo debe coincidir con el marcador.", "Partido.GolesMismatch");

    public static readonly Error GolEquipoNotInPartido =
        Error.Validation("Un gol está acreditado a un equipo que no juega este partido.", "Partido.GolEquipoNotInPartido");

    public static readonly Error ScorerNotInEquipo =
        Error.Validation("El goleador no pertenece al equipo al que se acredita el gol.", "Partido.ScorerNotInEquipo");

    public static readonly Error InvalidMinuto =
        Error.Validation("El minuto del gol debe estar entre 1 y 130.", "Partido.InvalidMinuto");

    public static readonly Error TorneoMismatch =
        Error.Validation("El partido no pertenece al torneo indicado.", "Partido.TorneoMismatch");
}
