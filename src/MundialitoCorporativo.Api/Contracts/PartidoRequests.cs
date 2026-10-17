using MundialitoCorporativo.Application.Partidos.Commands;
using MundialitoCorporativo.Domain.Partidos;

namespace MundialitoCorporativo.Api.Contracts;

/// <summary>
/// Cuerpo de POST /api/partidos. La fecha se guarda en UTC: si llega con zona horaria se convierte;
/// si llega sin zona se interpreta como UTC.
/// </summary>
public sealed record SchedulePartidoRequest(Guid TorneoId, Guid EquipoLocalId, Guid EquipoVisitanteId, DateTime FechaProgramada, string? Sede)
{
    public SchedulePartidoCommand ToCommand() =>
        new(TorneoId, EquipoLocalId, EquipoVisitanteId, UtcDate.From(FechaProgramada), Sede);
}

/// <summary>Cuerpo de PUT de partidos (reprogramación completa; el torneo no cambia).</summary>
public sealed record UpdatePartidoRequest(Guid EquipoLocalId, Guid EquipoVisitanteId, DateTime FechaProgramada, string? Sede)
{
    public UpdatePartidoCommand ToCommand(Guid id) =>
        new(id, EquipoLocalId, EquipoVisitanteId, UtcDate.From(FechaProgramada), Sede);
}

/// <summary>Cuerpo de PATCH de partidos: cambiar fecha o sede, o cancelar con estado "Cancelado".</summary>
public sealed record PatchPartidoRequest(DateTime? FechaProgramada, string? Sede, EstadoPartido? Estado)
{
    public PatchPartidoCommand ToCommand(Guid id) =>
        new(id, FechaProgramada is { } fecha ? UtcDate.From(fecha) : null, Sede, Estado);
}

/// <summary>Cuerpo de POST /api/partidos/{id}/resultado: marcador y un goleador por cada gol.</summary>
public sealed record RegisterPartidoResultRequest(int GolesLocal, int GolesVisitante, IReadOnlyList<GolRequest>? Goles)
{
    public RegisterPartidoResultCommand ToCommand(Guid partidoId) =>
        new(partidoId, GolesLocal, GolesVisitante,
            Goles?.Select(gol => new DetalleGol(gol.JugadorId, gol.EquipoId, gol.Minuto)).ToList()!);
}

public sealed record GolRequest(Guid JugadorId, Guid EquipoId, int Minuto);

internal static class UtcDate
{
    public static DateTime From(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
