using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Domain.Torneos;

/// <summary>
/// Torneo (edición del Mundialito, tabla Torneos) jugado en formato liga.
/// Controla la inscripción de equipos y su ciclo de vida.
/// </summary>
public sealed class Torneo : SoftDeletableAggregateRoot
{
    public const int NombreMinLength = 3;
    public const int NombreMaxLength = 100;
    public const int DescripcionMaxLength = 250;

    private readonly List<TorneoEquipo> _equipos = [];

    private Torneo(Guid id, string nombre, string? descripcion, DateOnly fechaInicio, DateOnly? fechaFin)
        : base(id)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
        Estado = EstadoTorneo.Planificado;
    }

    private Torneo()
    {
    }

    public string Nombre { get; private set; } = string.Empty;

    public string? Descripcion { get; private set; }

    public DateOnly FechaInicio { get; private set; }

    public DateOnly? FechaFin { get; private set; }

    public EstadoTorneo Estado { get; private set; }

    public IReadOnlyCollection<TorneoEquipo> Equipos => _equipos.AsReadOnly();

    public static Result<Torneo> Create(string nombre, string? descripcion, DateOnly fechaInicio, DateOnly? fechaFin)
    {
        var error = Validate(nombre, descripcion, fechaInicio, fechaFin);
        if (error is not null)
        {
            return error;
        }

        return new Torneo(SequentialGuid.NewGuid(), nombre.Trim(), Guard.NormalizeOptional(descripcion), fechaInicio, fechaFin);
    }

    public Result Update(string nombre, string? descripcion, DateOnly fechaInicio, DateOnly? fechaFin)
    {
        if (Estado == EstadoTorneo.Finalizado)
        {
            return TorneoErrors.AlreadyFinished;
        }

        var error = Validate(nombre, descripcion, fechaInicio, fechaFin);
        if (error is not null)
        {
            return error;
        }

        Nombre = nombre.Trim();
        Descripcion = Guard.NormalizeOptional(descripcion);
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;

        return Result.Success();
    }

    /// <summary>
    /// Cambia el estado del torneo. Solo se permite avanzar (Planificado → EnCurso → Finalizado);
    /// repetir el estado actual no produce cambios.
    /// </summary>
    public Result ChangeStatus(EstadoTorneo newStatus)
    {
        if (newStatus == Estado)
        {
            return Result.Success();
        }

        if (newStatus < Estado)
        {
            return TorneoErrors.InvalidStatusTransition(Estado, newStatus);
        }

        Estado = newStatus;
        return Result.Success();
    }

    public Result RegisterEquipo(Guid equipoId, DateTime utcNow)
    {
        if (Estado == EstadoTorneo.Finalizado)
        {
            return TorneoErrors.AlreadyFinished;
        }

        if (IsEquipoRegistered(equipoId))
        {
            return TorneoErrors.EquipoAlreadyRegistered;
        }

        _equipos.Add(new TorneoEquipo(Id, equipoId, utcNow));
        return Result.Success();
    }

    /// <summary>
    /// Retira un equipo. Quien invoca debe verificar antes que el equipo no tenga partidos en el torneo.
    /// </summary>
    public Result WithdrawEquipo(Guid equipoId)
    {
        if (Estado == EstadoTorneo.Finalizado)
        {
            return TorneoErrors.AlreadyFinished;
        }

        var registration = _equipos.FirstOrDefault(equipo => equipo.EquipoId == equipoId);
        if (registration is null)
        {
            return TorneoErrors.EquipoNotRegistered;
        }

        _equipos.Remove(registration);
        return Result.Success();
    }

    public bool IsEquipoRegistered(Guid equipoId) => _equipos.Any(equipo => equipo.EquipoId == equipoId);

    /// <summary>
    /// Verifica que el torneo admita programar un partido en la fecha indicada.
    /// </summary>
    public Result EnsureAcceptsPartidoOn(DateTime fechaProgramada)
    {
        if (Estado == EstadoTorneo.Finalizado)
        {
            return TorneoErrors.AlreadyFinished;
        }

        var date = DateOnly.FromDateTime(fechaProgramada);
        if (date < FechaInicio || (FechaFin is not null && date > FechaFin))
        {
            return TorneoErrors.DateOutOfRange;
        }

        return Result.Success();
    }

    /// <summary>
    /// Elimina lógicamente el torneo. Quien invoca debe verificar antes que no tenga partidos jugados.
    /// </summary>
    public Result Delete(DateTime utcNow)
    {
        MarkAsDeleted(utcNow);
        return Result.Success();
    }

    private static Error? Validate(string nombre, string? descripcion, DateOnly fechaInicio, DateOnly? fechaFin) =>
        Guard.FirstError(
            Guard.Text(nombre, NombreMinLength, NombreMaxLength, TorneoErrors.InvalidNombre),
            Guard.OptionalText(descripcion, DescripcionMaxLength, TorneoErrors.DescripcionTooLong),
            fechaFin is not null && fechaFin < fechaInicio ? TorneoErrors.InvalidDateRange : null);
}
