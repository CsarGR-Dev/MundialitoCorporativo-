using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Jugadores.Events;
using MundialitoCorporativo.Domain.PosicionesJuego;

namespace MundialitoCorporativo.Domain.Jugadores;

/// <summary>
/// Integrante de un equipo (tabla Jugadores). No cambia de equipo una vez registrado (RN-17).
/// </summary>
public sealed class Jugador : SoftDeletableAggregateRoot
{
    public const int NombreMinLength = 2;
    public const int NombreMaxLength = 60;
    public const int NumeroCamisetaMin = 1;
    public const int NumeroCamisetaMax = 99;

    private Jugador(Guid id, Guid equipoId, byte posicionJuegoId, string nombres, string apellidos, byte numeroCamiseta)
        : base(id)
    {
        EquipoId = equipoId;
        PosicionJuegoId = posicionJuegoId;
        Nombres = nombres;
        Apellidos = apellidos;
        NumeroCamiseta = numeroCamiseta;
        Activo = true;
    }

    private Jugador()
    {
    }

    public Guid EquipoId { get; private init; }

    public byte PosicionJuegoId { get; private set; }

    public string Nombres { get; private set; } = string.Empty;

    public string Apellidos { get; private set; } = string.Empty;

    public byte NumeroCamiseta { get; private set; }

    public bool Activo { get; private set; }

    /// <summary>
    /// Registra un jugador en un equipo. La unicidad del número de camiseta (RN-02) la valida quien invoca.
    /// </summary>
    public static Result<Jugador> Create(Guid equipoId, PosicionJuego posicionJuego, string nombres, string apellidos, int numeroCamiseta)
    {
        var error = Validate(posicionJuego, nombres, apellidos, numeroCamiseta);
        if (error is not null)
        {
            return error;
        }

        var jugador = new Jugador(SequentialGuid.NewGuid(), equipoId, posicionJuego.Id, nombres.Trim(), apellidos.Trim(), (byte)numeroCamiseta);
        jugador.Raise(new JugadorRegisteredDomainEvent(jugador.Id, equipoId, numeroCamiseta));

        return jugador;
    }

    public Result Update(PosicionJuego posicionJuego, string nombres, string apellidos, int numeroCamiseta, bool activo)
    {
        // Una posición ya asignada puede conservarse aunque luego se haya desactivado en el catálogo.
        var error = posicionJuego.Id == PosicionJuegoId
            ? Guard.FirstError(ValidateNombres(nombres, apellidos), ValidateNumeroCamiseta(numeroCamiseta))
            : Validate(posicionJuego, nombres, apellidos, numeroCamiseta);

        if (error is not null)
        {
            return error;
        }

        PosicionJuegoId = posicionJuego.Id;
        Nombres = nombres.Trim();
        Apellidos = apellidos.Trim();
        NumeroCamiseta = (byte)numeroCamiseta;
        Activo = activo;

        return Result.Success();
    }

    public Result Delete(DateTime utcNow)
    {
        MarkAsDeleted(utcNow);
        return Result.Success();
    }

    private static Error? Validate(PosicionJuego posicionJuego, string nombres, string apellidos, int numeroCamiseta) =>
        Guard.FirstError(
            posicionJuego.Activo ? null : PosicionJuegoErrors.Inactive(posicionJuego.Id),
            ValidateNombres(nombres, apellidos),
            ValidateNumeroCamiseta(numeroCamiseta));

    private static Error? ValidateNombres(string nombres, string apellidos) =>
        Guard.FirstError(
            Guard.Text(nombres, NombreMinLength, NombreMaxLength, JugadorErrors.InvalidNombres),
            Guard.Text(apellidos, NombreMinLength, NombreMaxLength, JugadorErrors.InvalidApellidos));

    private static Error? ValidateNumeroCamiseta(int numeroCamiseta) =>
        numeroCamiseta is < NumeroCamisetaMin or > NumeroCamisetaMax ? JugadorErrors.InvalidNumeroCamiseta : null;
}
