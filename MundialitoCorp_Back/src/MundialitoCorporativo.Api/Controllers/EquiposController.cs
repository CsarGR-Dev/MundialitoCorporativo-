using MediatR;
using Microsoft.AspNetCore.Mvc;
using MundialitoCorporativo.Api.Contracts;
using MundialitoCorporativo.Api.Http;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Equipos;
using MundialitoCorporativo.Application.Equipos.Commands;
using MundialitoCorporativo.Application.Equipos.Queries;
using MundialitoCorporativo.Application.Jugadores;
using MundialitoCorporativo.Application.Jugadores.Queries;

namespace MundialitoCorporativo.Api.Controllers;

/// <summary>
/// Equipos participantes: CRUD completo, filtros, paginación y ordenamiento dinámico; integrantes por equipo.
/// </summary>
[Route("api/equipos")]
public sealed class EquiposController(ISender sender) : ApiController(sender)
{
    /// <summary>Lista paginada. Filtros: nombre (o abreviatura), departamento.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<EquipoResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] GetEquiposQuery query, CancellationToken cancellationToken) =>
        QueryAsync(query, cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<EquipoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        QueryAsync(new GetEquipoByIdQuery(id), cancellationToken);

    /// <summary>Crea un equipo. Requiere el encabezado Idempotency-Key.</summary>
    [HttpPost]
    [ProducesResponseType<EquipoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public async Task<IActionResult> Create(EquipoRequest request, CancellationToken cancellationToken) =>
        await CreatedAsync(
            await Sender.Send(request.ToCreateCommand(), cancellationToken),
            id => new GetEquipoByIdQuery(id),
            nameof(GetById),
            null,
            cancellationToken);

    /// <summary>Reemplazo completo (idempotente).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<EquipoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> Update(Guid id, EquipoRequest request, CancellationToken cancellationToken) =>
        SendThenQueryAsync(request.ToUpdateCommand(id), new GetEquipoByIdQuery(id), cancellationToken);

    /// <summary>Modificación parcial: solo los campos enviados.</summary>
    [HttpPatch("{id:guid}")]
    [Consumes(MediaTypes.Json, MediaTypes.MergePatchJson)]
    [ProducesResponseType<EquipoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> Patch(Guid id, PatchEquipoRequest request, CancellationToken cancellationToken) =>
        SendThenQueryAsync(request.ToCommand(id), new GetEquipoByIdQuery(id), cancellationToken);

    /// <summary>Eliminación lógica idempotente: 204 también si ya estaba eliminado; 404 si nunca existió.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        SendNoContentAsync(new DeleteEquipoCommand(id), cancellationToken);

    /// <summary>Integrantes del equipo, paginados. Filtros: nombre, posicionJuegoId, activo.</summary>
    [HttpGet("{id:guid}/jugadores")]
    [ProducesResponseType<PagedResponse<JugadorResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    public Task<IActionResult> GetJugadores(Guid id, [FromQuery] GetJugadoresQuery query, CancellationToken cancellationToken) =>
        QueryAsync(query with { EquipoId = id, RequireEquipo = true }, cancellationToken);

    /// <summary>Registra un integrante en el equipo. Requiere el encabezado Idempotency-Key.</summary>
    [HttpPost("{id:guid}/jugadores")]
    [ProducesResponseType<JugadorResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public async Task<IActionResult> RegisterJugador(Guid id, RegisterJugadorRequest request, CancellationToken cancellationToken) =>
        await CreatedAsync(
            await Sender.Send(request.ToCommand(id), cancellationToken),
            jugadorId => new GetJugadorByIdQuery(jugadorId),
            nameof(JugadoresController.GetById),
            "Jugadores",
            cancellationToken);
}
