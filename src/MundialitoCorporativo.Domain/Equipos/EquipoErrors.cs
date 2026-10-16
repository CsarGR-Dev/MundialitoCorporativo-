using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Domain.Equipos;

public static class EquipoErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound($"No existe el equipo '{id}'.", "Equipo.NotFound");

    public static Error DuplicateNombre(string nombre) =>
        Error.Conflict($"Ya existe un equipo con el nombre '{nombre}'.", "Equipo.DuplicateNombre");

    public static Error DuplicateAbreviatura(string abreviatura) =>
        Error.Conflict($"Ya existe un equipo con la abreviatura '{abreviatura}'.", "Equipo.DuplicateAbreviatura");

    public static readonly Error InvalidNombre =
        Error.Validation("El nombre del equipo es obligatorio y debe tener entre 3 y 100 caracteres.", "Equipo.InvalidNombre");

    public static readonly Error InvalidAbreviatura =
        Error.Validation("La abreviatura del equipo es obligatoria y debe tener entre 2 y 5 caracteres.", "Equipo.InvalidAbreviatura");

    public static readonly Error DepartamentoTooLong =
        Error.Validation("El departamento no puede superar 100 caracteres.", "Equipo.DepartamentoTooLong");

    public static readonly Error HasPartidos =
        Error.Conflict("El equipo tiene partidos asociados y no puede eliminarse.", "Equipo.HasPartidos");
}
