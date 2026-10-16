using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Jugadores;
using MundialitoCorporativo.Domain.PosicionesJuego;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.UnitTests.Domain;

/// <summary>
/// Datos de prueba reutilizables para las pruebas de dominio.
/// </summary>
internal static class TestData
{
    public static readonly DateTime UtcNow = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    public static readonly PosicionJuego Delantero = new(4, "Delantero", "DEL", 4);

    public static readonly PosicionJuego PosicionInactiva = new(9, "Líbero", "LIB", 9, activo: false);

    public static Torneo NewTorneo(DateOnly? fechaInicio = null, DateOnly? fechaFin = null) =>
        Torneo.Create("Mundialito Corporativo 2026", null, fechaInicio ?? new DateOnly(2026, 10, 1), fechaFin).Value;

    public static Equipo NewEquipo(string nombre, string abreviatura) =>
        Equipo.Create(nombre, abreviatura, null).Value;

    public static Jugador NewJugador(Guid equipoId, int numeroCamiseta) =>
        Jugador.Create(equipoId, Delantero, "Ana", "López", numeroCamiseta).Value;
}
