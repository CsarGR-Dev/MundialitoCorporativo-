using Dapper;
using Microsoft.Extensions.Logging;
using MundialitoCorporativo.Application.Abstractions.Data;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Jugadores;
using MundialitoCorporativo.Domain.Partidos;
using MundialitoCorporativo.Domain.PosicionesJuego;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Infrastructure.Seed;

/// <summary>
/// Seed automático (requerimiento ítem 9): si la base está vacía carga <see cref="SeedData"/> con las mismas
/// reglas de dominio que la API y lo confirma con el Unit of Work en una sola transacción.
/// Así también quedan registrados los eventos de dominio (equipos creados, resultados registrados).
/// </summary>
internal sealed partial class DatabaseSeeder(
    IDbConnectionFactory connectionFactory,
    ITorneoRepository torneoRepository,
    IEquipoRepository equipoRepository,
    IJugadorRepository jugadorRepository,
    IPartidoRepository partidoRepository,
    IPosicionJuegoRepository posicionJuegoRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<DatabaseSeeder> logger)
{
    /// <summary>
    /// Vacía = sin torneos ni equipos, contando también los eliminados lógicamente: si alguien borró los datos
    /// del seed, no se vuelven a cargar.
    /// </summary>
    private const string HasDataSql = """
        SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Torneos) OR EXISTS (SELECT 1 FROM dbo.Equipos) THEN 1 ELSE 0 END;
        """;

    /// <returns><c>true</c> si se cargaron los datos.</returns>
    public async Task<bool> SeedIfEmptyAsync(CancellationToken cancellationToken)
    {
        await using (var connection = await connectionFactory.OpenConnectionAsync(cancellationToken))
        {
            var hasData = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(HasDataSql, cancellationToken: cancellationToken));
            if (hasData)
            {
                LogSeedSkipped(logger);
                return false;
            }
        }

        var counts = await AddSeedDataAsync(cancellationToken);

        var result = await unitOfWork.CommitAsync(cancellationToken);
        if (result.IsFailure)
        {
            // Otra instancia pudo cargar los mismos datos al mismo tiempo: los índices únicos lo impiden.
            LogSeedConflict(logger, result.ErrorCode);
            return false;
        }

        LogSeedCompleted(logger, 1, counts.Equipos, counts.Jugadores, counts.Partidos, counts.PartidosJugados);
        return true;
    }

    private async Task<(int Equipos, int Jugadores, int Partidos, int PartidosJugados)> AddSeedDataAsync(CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);

        var torneo = Require(Torneo.Create(
            SeedData.TorneoNombre,
            SeedData.TorneoDescripcion,
            today.AddDays(SeedData.TorneoInicioDias),
            today.AddDays(SeedData.TorneoFinDias)));
        Require(torneo.ChangeStatus(EstadoTorneo.EnCurso));
        torneoRepository.Add(torneo);

        var posiciones = new Dictionary<byte, PosicionJuego>();
        var equipos = new List<Equipo>();
        var jugadores = new Dictionary<(int Equipo, int NumeroCamiseta), Jugador>();

        for (var equipoIndex = 0; equipoIndex < SeedData.Equipos.Count; equipoIndex++)
        {
            var equipoSeed = SeedData.Equipos[equipoIndex];
            var equipo = Require(Equipo.Create(equipoSeed.Nombre, equipoSeed.Abreviatura, equipoSeed.Departamento));
            equipoRepository.Add(equipo);
            Require(torneo.RegisterEquipo(equipo.Id, utcNow));
            equipos.Add(equipo);

            foreach (var jugadorSeed in equipoSeed.Jugadores)
            {
                var posicion = await GetPosicionAsync(posiciones, jugadorSeed.PosicionJuegoId, cancellationToken);
                var jugador = Require(Jugador.Create(equipo.Id, posicion, jugadorSeed.Nombres, jugadorSeed.Apellidos, jugadorSeed.NumeroCamiseta));
                jugadorRepository.Add(jugador);
                jugadores[(equipoIndex, jugadorSeed.NumeroCamiseta)] = jugador;
            }
        }

        var playedCount = 0;
        foreach (var partidoSeed in SeedData.Partidos)
        {
            var fechaProgramada = today.AddDays(partidoSeed.Dias).ToDateTime(new TimeOnly(SeedData.HoraPartidoUtc, 0), DateTimeKind.Utc);
            var partido = Require(Partido.Schedule(
                torneo,
                equipos[partidoSeed.Local].Id,
                equipos[partidoSeed.Visitante].Id,
                fechaProgramada,
                partidoSeed.Sede));

            if (partidoSeed.Resultado is { } resultado)
            {
                var goleadores = resultado.Goles.Select(gol => jugadores[(gol.Equipo, gol.NumeroCamiseta)]).Distinct().ToList();
                var goles = resultado.Goles
                    .Select(gol => new DetalleGol(jugadores[(gol.Equipo, gol.NumeroCamiseta)].Id, equipos[gol.Equipo].Id, gol.Minuto))
                    .ToList();

                // El partido se registra como jugado dos horas después de su inicio.
                Require(partido.RegisterResult(torneo, resultado.GolesLocal, resultado.GolesVisitante, goles, goleadores, fechaProgramada.AddHours(2)));
                playedCount++;
            }

            partidoRepository.Add(partido);
        }

        return (equipos.Count, jugadores.Count, SeedData.Partidos.Count, playedCount);
    }

    private async Task<PosicionJuego> GetPosicionAsync(Dictionary<byte, PosicionJuego> cache, byte id, CancellationToken cancellationToken)
    {
        if (!cache.TryGetValue(id, out var posicion))
        {
            posicion = await posicionJuegoRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new InvalidOperationException($"Falta la posición de juego {id} del catálogo (001_DDL_CrearBaseDatos.sql).");
            cache[id] = posicion;
        }

        return posicion;
    }

    /// <summary>
    /// Los datos del seed son fijos: si una regla de dominio los rechaza es un error de programación, no de negocio.
    /// </summary>
    private static T Require<T>(Result<T> result) =>
        result.IsSuccess ? result.Value : throw new InvalidOperationException($"Datos de seed inválidos: {result.ErrorMessage}");

    private static void Require(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Datos de seed inválidos: {result.ErrorMessage}");
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "La base ya tiene datos; no se ejecuta el seed")]
    private static partial void LogSeedSkipped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "El seed no se confirmó ({ErrorCode}); otra instancia pudo cargarlo al mismo tiempo")]
    private static partial void LogSeedConflict(ILogger logger, string? errorCode);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Seed ejecutado: {Torneos} torneo, {Equipos} equipos, {Jugadores} jugadores, {Partidos} partidos ({PartidosJugados} con resultado)")]
    private static partial void LogSeedCompleted(ILogger logger, int torneos, int equipos, int jugadores, int partidos, int partidosJugados);
}
