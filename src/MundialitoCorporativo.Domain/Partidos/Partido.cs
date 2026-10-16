using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Jugadores;
using MundialitoCorporativo.Domain.Partidos.Events;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Domain.Partidos;

/// <summary>
/// Partido de un torneo (tabla Partidos): calendario, resultado y goles.
/// </summary>
public sealed class Partido : SoftDeletableAggregateRoot
{
    public const int SedeMaxLength = 100;
    public const int GolesMax = 99;
    public const int MinutoMin = 1;
    public const int MinutoMax = 130;

    private readonly List<Gol> _goles = [];

    private Partido(Guid id, Guid torneoId, Guid equipoLocalId, Guid equipoVisitanteId, DateTime fechaProgramada, string? sede)
        : base(id)
    {
        TorneoId = torneoId;
        EquipoLocalId = equipoLocalId;
        EquipoVisitanteId = equipoVisitanteId;
        FechaProgramada = fechaProgramada;
        Sede = sede;
        Estado = EstadoPartido.Programado;
    }

    private Partido()
    {
    }

    public Guid TorneoId { get; private init; }

    public Guid EquipoLocalId { get; private set; }

    public Guid EquipoVisitanteId { get; private set; }

    /// <summary>Fecha y hora programada en UTC, con precisión de segundos.</summary>
    public DateTime FechaProgramada { get; private set; }

    public string? Sede { get; private set; }

    public EstadoPartido Estado { get; private set; }

    public int? GolesLocal { get; private set; }

    public int? GolesVisitante { get; private set; }

    public DateTime? FechaJugado { get; private set; }

    public IReadOnlyCollection<Gol> Goles => _goles.AsReadOnly();

    /// <summary>
    /// Programa un partido. Que ningún equipo tenga otro partido a la misma hora (RN-05) lo valida quien invoca.
    /// </summary>
    public static Result<Partido> Schedule(Torneo torneo, Guid equipoLocalId, Guid equipoVisitanteId, DateTime fechaProgramada, string? sede)
    {
        var scheduledAt = TruncateToSeconds(fechaProgramada);

        var validation = ValidateSchedule(torneo, equipoLocalId, equipoVisitanteId, scheduledAt, sede);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        var partido = new Partido(SequentialGuid.NewGuid(), torneo.Id, equipoLocalId, equipoVisitanteId, scheduledAt, Guard.NormalizeOptional(sede));

        partido.Raise(new PartidoScheduledDomainEvent(partido.Id, partido.TorneoId, equipoLocalId, equipoVisitanteId, partido.FechaProgramada));

        return partido;
    }

    /// <summary>
    /// Reemplaza los datos de programación (PUT). Solo para partidos en estado programado.
    /// </summary>
    public Result Update(Torneo torneo, Guid equipoLocalId, Guid equipoVisitanteId, DateTime fechaProgramada, string? sede)
    {
        if (Estado != EstadoPartido.Programado)
        {
            return PartidoErrors.NotScheduled;
        }

        if (torneo.Id != TorneoId)
        {
            return PartidoErrors.TorneoMismatch;
        }

        var scheduledAt = TruncateToSeconds(fechaProgramada);

        var validation = ValidateSchedule(torneo, equipoLocalId, equipoVisitanteId, scheduledAt, sede);
        if (validation.IsFailure)
        {
            return validation;
        }

        EquipoLocalId = equipoLocalId;
        EquipoVisitanteId = equipoVisitanteId;
        FechaProgramada = scheduledAt;
        Sede = Guard.NormalizeOptional(sede);

        return Result.Success();
    }

    /// <summary>
    /// Cancela el partido. Cancelar un partido ya cancelado no produce cambios.
    /// </summary>
    public Result Cancel()
    {
        switch (Estado)
        {
            case EstadoPartido.Jugado:
                return PartidoErrors.PlayedCannotBeCancelled;
            case EstadoPartido.Cancelado:
                return Result.Success();
            default:
                Estado = EstadoPartido.Cancelado;
                return Result.Success();
        }
    }

    /// <summary>
    /// Registra el resultado y los goleadores del partido.
    /// </summary>
    /// <param name="torneo">Torneo del partido (no debe estar finalizado).</param>
    /// <param name="golesLocal">Goles del equipo local.</param>
    /// <param name="golesVisitante">Goles del equipo visitante.</param>
    /// <param name="goles">Detalle de cada gol: jugador, equipo y minuto.</param>
    /// <param name="scorers">Jugadores referenciados en <paramref name="goles"/>, cargados por quien invoca.</param>
    /// <param name="utcNow">Momento del registro.</param>
    public Result RegisterResult(
        Torneo torneo,
        int golesLocal,
        int golesVisitante,
        IReadOnlyCollection<DetalleGol> goles,
        IReadOnlyCollection<Jugador> scorers,
        DateTime utcNow)
    {
        var stateCheck = EnsureCanRegisterResult(torneo);
        if (stateCheck.IsFailure)
        {
            return stateCheck;
        }

        var golesCheck = ValidateGoles(golesLocal, golesVisitante, goles, scorers);
        if (golesCheck.IsFailure)
        {
            return golesCheck;
        }

        foreach (var gol in goles)
        {
            _goles.Add(new Gol(SequentialGuid.NewGuid(), Id, gol.JugadorId, gol.EquipoId, (byte)gol.Minuto));
        }

        GolesLocal = golesLocal;
        GolesVisitante = golesVisitante;
        FechaJugado = utcNow;
        Estado = EstadoPartido.Jugado;

        Raise(new PartidoResultRegisteredDomainEvent(Id, TorneoId, EquipoLocalId, EquipoVisitanteId, golesLocal, golesVisitante));

        return Result.Success();
    }

    /// <summary>
    /// Elimina lógicamente el partido. Un partido jugado no puede eliminarse (RN-12).
    /// </summary>
    public Result Delete(DateTime utcNow)
    {
        if (Estado == EstadoPartido.Jugado)
        {
            return PartidoErrors.PlayedCannotBeDeleted;
        }

        MarkAsDeleted(utcNow);
        return Result.Success();
    }

    public bool Involves(Guid equipoId) => EquipoLocalId == equipoId || EquipoVisitanteId == equipoId;

    private Result EnsureCanRegisterResult(Torneo torneo)
    {
        if (torneo.Id != TorneoId)
        {
            return PartidoErrors.TorneoMismatch;
        }

        if (torneo.Estado == EstadoTorneo.Finalizado)
        {
            return TorneoErrors.AlreadyFinished;
        }

        return Estado switch
        {
            EstadoPartido.Jugado => PartidoErrors.AlreadyPlayed,
            EstadoPartido.Cancelado => PartidoErrors.IsCancelled,
            _ => Result.Success()
        };
    }

    private Result ValidateGoles(int golesLocal, int golesVisitante, IReadOnlyCollection<DetalleGol> goles, IReadOnlyCollection<Jugador> scorers)
    {
        if (golesLocal is < 0 or > GolesMax || golesVisitante is < 0 or > GolesMax)
        {
            return PartidoErrors.InvalidScore;
        }

        foreach (var gol in goles)
        {
            if (gol.Minuto is < MinutoMin or > MinutoMax)
            {
                return PartidoErrors.InvalidMinuto;
            }

            if (!Involves(gol.EquipoId))
            {
                return PartidoErrors.GolEquipoNotInPartido;
            }

            var scorer = scorers.FirstOrDefault(jugador => jugador.Id == gol.JugadorId && !jugador.Eliminado);
            if (scorer is null)
            {
                return JugadorErrors.NotFound(gol.JugadorId);
            }

            if (scorer.EquipoId != gol.EquipoId)
            {
                return PartidoErrors.ScorerNotInEquipo;
            }
        }

        var homeCount = goles.Count(gol => gol.EquipoId == EquipoLocalId);
        var awayCount = goles.Count(gol => gol.EquipoId == EquipoVisitanteId);

        return homeCount == golesLocal && awayCount == golesVisitante
            ? Result.Success()
            : PartidoErrors.GolesMismatch;
    }

    private static Result ValidateSchedule(Torneo torneo, Guid equipoLocalId, Guid equipoVisitanteId, DateTime fechaProgramada, string? sede)
    {
        if (equipoLocalId == equipoVisitanteId)
        {
            return PartidoErrors.SameEquipo;
        }

        if (!torneo.IsEquipoRegistered(equipoLocalId) || !torneo.IsEquipoRegistered(equipoVisitanteId))
        {
            return TorneoErrors.EquipoNotRegistered;
        }

        var sedeError = Guard.OptionalText(sede, SedeMaxLength, PartidoErrors.SedeTooLong);
        if (sedeError is not null)
        {
            return sedeError;
        }

        return torneo.EnsureAcceptsPartidoOn(fechaProgramada);
    }

    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond), value.Kind);
}
