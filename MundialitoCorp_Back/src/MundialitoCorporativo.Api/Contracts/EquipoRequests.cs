using MundialitoCorporativo.Application.Equipos.Commands;
using MundialitoCorporativo.Application.Jugadores.Commands;

namespace MundialitoCorporativo.Api.Contracts;

/// <summary>Cuerpo de POST y PUT de equipos (PUT reemplaza todos los campos).</summary>
public sealed record EquipoRequest(string Nombre, string Abreviatura, string? Departamento)
{
    public CreateEquipoCommand ToCreateCommand() => new(Nombre, Abreviatura, Departamento);

    public UpdateEquipoCommand ToUpdateCommand(Guid id) => new(id, Nombre, Abreviatura, Departamento);
}

/// <summary>Cuerpo de PATCH de equipos: solo se modifican los campos enviados.</summary>
public sealed record PatchEquipoRequest(string? Nombre, string? Abreviatura, string? Departamento)
{
    public PatchEquipoCommand ToCommand(Guid id) => new(id, Nombre, Abreviatura, Departamento);
}

/// <summary>Cuerpo de POST /api/equipos/{id}/jugadores (registro de integrantes por equipo).</summary>
public sealed record RegisterJugadorRequest(byte PosicionJuegoId, string Nombres, string Apellidos, int NumeroCamiseta)
{
    public RegisterJugadorCommand ToCommand(Guid equipoId) => new(equipoId, PosicionJuegoId, Nombres, Apellidos, NumeroCamiseta);
}
