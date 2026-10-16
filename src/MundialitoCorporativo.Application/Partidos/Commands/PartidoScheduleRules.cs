using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Partidos;

namespace MundialitoCorporativo.Application.Partidos.Commands;

/// <summary>
/// RN-05: ningún equipo puede tener dos partidos activos a la misma hora, como local o como visitante.
/// La base de datos cubre el mismo rol (local/local, visitante/visitante); esta verificación cubre el cruce.
/// </summary>
internal static class PartidoScheduleRules
{
    public static async Task<Error?> CheckAvailabilityAsync(
        IPartidoRepository partidoRepository,
        Guid equipoLocalId,
        Guid equipoVisitanteId,
        DateTime fechaProgramada,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        await partidoRepository.ExistsAtSameTimeAsync(equipoLocalId, equipoVisitanteId, TruncateToSeconds(fechaProgramada), excludeId, cancellationToken)
            ? PartidoErrors.EquipoAlreadyHasPartidoAtTime
            : null;

    // Misma precisión con la que se guarda la fecha (datetime2(0)).
    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond), value.Kind);
}
