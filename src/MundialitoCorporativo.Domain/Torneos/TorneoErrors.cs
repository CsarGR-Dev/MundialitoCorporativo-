using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Domain.Torneos;

public static class TorneoErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound($"No existe el torneo '{id}'.", "Torneo.NotFound");

    public static Error DuplicateNombre(string nombre) =>
        Error.Conflict($"Ya existe un torneo con el nombre '{nombre}'.", "Torneo.DuplicateNombre");

    public static readonly Error InvalidNombre =
        Error.Validation("El nombre del torneo es obligatorio y debe tener entre 3 y 100 caracteres.", "Torneo.InvalidNombre");

    public static readonly Error DescripcionTooLong =
        Error.Validation("La descripción del torneo no puede superar 250 caracteres.", "Torneo.DescripcionTooLong");

    public static readonly Error InvalidDateRange =
        Error.Validation("La fecha de fin no puede ser anterior a la fecha de inicio.", "Torneo.InvalidDateRange");

    public static Error InvalidStatusTransition(EstadoTorneo from, EstadoTorneo to) =>
        Error.Conflict($"El torneo no puede pasar de '{from}' a '{to}'.", "Torneo.InvalidStatusTransition");

    public static readonly Error AlreadyFinished =
        Error.Conflict("El torneo está finalizado y no admite cambios.", "Torneo.AlreadyFinished");

    public static readonly Error EquipoAlreadyRegistered =
        Error.Conflict("El equipo ya está inscrito en el torneo.", "Torneo.EquipoAlreadyRegistered");

    public static readonly Error EquipoNotRegistered =
        Error.Validation("El equipo no está inscrito en el torneo.", "Torneo.EquipoNotRegistered");

    public static Error InscripcionNotFound(Guid equipoId) =>
        Error.NotFound($"El equipo '{equipoId}' no está inscrito en el torneo.", "Torneo.InscripcionNotFound");

    public static readonly Error EquipoHasPartidos =
        Error.Conflict("El equipo tiene partidos en el torneo y no puede retirarse.", "Torneo.EquipoHasPartidos");

    public static readonly Error HasPlayedPartidos =
        Error.Conflict("El torneo tiene partidos jugados y no puede eliminarse.", "Torneo.HasPlayedPartidos");

    public static readonly Error DateOutOfRange =
        Error.Validation("La fecha del partido está fuera del rango de fechas del torneo.", "Torneo.DateOutOfRange");
}
