using MundialitoCorporativo.Application.Torneos.Commands;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Api.Contracts;

/// <summary>Cuerpo de POST y PUT de torneos (PUT reemplaza todos los campos).</summary>
public sealed record TorneoRequest(string Nombre, string? Descripcion, DateOnly FechaInicio, DateOnly? FechaFin)
{
    public CreateTorneoCommand ToCreateCommand() => new(Nombre, Descripcion, FechaInicio, FechaFin);

    public UpdateTorneoCommand ToUpdateCommand(Guid id) => new(id, Nombre, Descripcion, FechaInicio, FechaFin);
}

/// <summary>Cuerpo de PATCH de torneos: modifica los datos enviados o cambia el estado (Planificado → EnCurso → Finalizado).</summary>
public sealed record PatchTorneoRequest(string? Nombre, string? Descripcion, DateOnly? FechaInicio, DateOnly? FechaFin, EstadoTorneo? Estado)
{
    public PatchTorneoCommand ToCommand(Guid id) => new(id, Nombre, Descripcion, FechaInicio, FechaFin, Estado);
}

/// <summary>Cuerpo de POST /api/torneos/{id}/equipos (inscripción de un equipo participante).</summary>
public sealed record RegisterEquipoInTorneoRequest(Guid EquipoId)
{
    public RegisterEquipoInTorneoCommand ToCommand(Guid torneoId) => new(torneoId, EquipoId);
}

/// <summary>Respuesta de la inscripción de un equipo en un torneo.</summary>
public sealed record InscripcionEquipoResponse(Guid TorneoId, Guid EquipoId);
