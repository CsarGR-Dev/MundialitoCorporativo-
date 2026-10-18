using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos.Events;

namespace MundialitoCorporativo.Domain.Equipos;

/// <summary>
/// Equipo de la empresa (tabla Equipos). Existe una sola vez y puede inscribirse en varios torneos.
/// </summary>
public sealed class Equipo : SoftDeletableAggregateRoot
{
    public const int NombreMinLength = 3;
    public const int NombreMaxLength = 100;
    public const int AbreviaturaMinLength = 2;
    public const int AbreviaturaMaxLength = 5;
    public const int DepartamentoMaxLength = 100;

    private Equipo(Guid id, string nombre, string abreviatura, string? departamento)
        : base(id)
    {
        Nombre = nombre;
        Abreviatura = abreviatura;
        Departamento = departamento;
    }

    private Equipo()
    {
    }

    public string Nombre { get; private set; } = string.Empty;

    public string Abreviatura { get; private set; } = string.Empty;

    public string? Departamento { get; private set; }

    /// <summary>
    /// Crea un equipo. La unicidad de nombre y abreviatura (RN-01) la valida quien invoca contra el repositorio.
    /// </summary>
    public static Result<Equipo> Create(string nombre, string abreviatura, string? departamento)
    {
        var error = Validate(nombre, abreviatura, departamento);
        if (error is not null)
        {
            return error;
        }

        var equipo = new Equipo(SequentialGuid.NewGuid(), nombre.Trim(), NormalizeAbreviatura(abreviatura), Guard.NormalizeOptional(departamento));
        equipo.Raise(new EquipoCreatedDomainEvent(equipo.Id, equipo.Nombre, equipo.Abreviatura));

        return equipo;
    }

    public Result Update(string nombre, string abreviatura, string? departamento)
    {
        var error = Validate(nombre, abreviatura, departamento);
        if (error is not null)
        {
            return error;
        }

        Nombre = nombre.Trim();
        Abreviatura = NormalizeAbreviatura(abreviatura);
        Departamento = Guard.NormalizeOptional(departamento);

        return Result.Success();
    }

    /// <summary>
    /// Elimina lógicamente el equipo. Quien invoca debe verificar antes que no tenga partidos (RN-11).
    /// </summary>
    public Result Delete(DateTime utcNow)
    {
        MarkAsDeleted(utcNow);
        return Result.Success();
    }

    public static string NormalizeAbreviatura(string abreviatura) => abreviatura.Trim().ToUpperInvariant();

    private static Error? Validate(string nombre, string abreviatura, string? departamento) =>
        Guard.FirstError(
            Guard.Text(nombre, NombreMinLength, NombreMaxLength, EquipoErrors.InvalidNombre),
            Guard.Text(abreviatura, AbreviaturaMinLength, AbreviaturaMaxLength, EquipoErrors.InvalidAbreviatura),
            Guard.OptionalText(departamento, DepartamentoMaxLength, EquipoErrors.DepartamentoTooLong));
}
