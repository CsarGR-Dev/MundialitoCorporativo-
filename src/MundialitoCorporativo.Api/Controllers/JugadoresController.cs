using Microsoft.AspNetCore.Mvc;
using MundialitoCorporativo.Api.Contracts;
using MundialitoCorporativo.Api.Http;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Jugadores;
using MundialitoCorporativo.Application.Jugadores.Commands;
using MundialitoCorporativo.Application.Jugadores.Queries;

namespace MundialitoCorporativo.Api.Controllers;

/// <summary>
/// Integrantes de equipos. El registro se hace por equipo: POST /api/equipos/{id}/jugadores.
/// </summary>
[Route("api/jugadores")]
public sealed class JugadoresController(IDispatcher dispatcher) : ApiController(dispatcher)
{
    /// <summary>Lista paginada de todos los jugadores. Filtros: equipoId, nombre, posicionJuegoId, activo.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<JugadorResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] GetJugadoresQuery query, CancellationToken cancellationToken) =>
        QueryAsync(query, cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<JugadorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        QueryAsync(new GetJugadorByIdQuery(id), cancellationToken);

    /// <summary>Reemplazo completo (idempotente). El equipo del jugador no cambia.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<JugadorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> Update(Guid id, UpdateJugadorRequest request, CancellationToken cancellationToken) =>
        SendThenQueryAsync(request.ToCommand(id), new GetJugadorByIdQuery(id), cancellationToken);

    /// <summary>Modificación parcial: solo los campos enviados.</summary>
    [HttpPatch("{id:guid}")]
    [Consumes(MediaTypes.Json, MediaTypes.MergePatchJson)]
    [ProducesResponseType<JugadorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> Patch(Guid id, PatchJugadorRequest request, CancellationToken cancellationToken) =>
        SendThenQueryAsync(request.ToCommand(id), new GetJugadorByIdQuery(id), cancellationToken);

    /// <summary>Eliminación lógica idempotente. Sus goles se conservan en el historial.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        SendNoContentAsync(new DeleteJugadorCommand(id), cancellationToken);
}
