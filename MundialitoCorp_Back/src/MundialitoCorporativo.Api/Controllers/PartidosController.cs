using Microsoft.AspNetCore.Mvc;
using MundialitoCorporativo.Api.Contracts;
using MundialitoCorporativo.Api.Http;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Partidos;
using MundialitoCorporativo.Application.Partidos.Commands;
using MundialitoCorporativo.Application.Partidos.Queries;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Api.Controllers;

/// <summary>
/// Calendario de partidos y registro de resultados.
/// </summary>
[Route("api/partidos")]
public sealed class PartidosController(IDispatcher dispatcher) : ApiController(dispatcher)
{
    /// <summary>Lista paginada. Filtros: torneoId, fechaDesde, fechaHasta, equipoId (local o visitante), estado.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<PartidoResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] GetPartidosQuery query, CancellationToken cancellationToken) =>
        QueryAsync(query, cancellationToken);

    /// <summary>Detalle del partido con su marcador y goleadores.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PartidoDetalleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        QueryAsync(new GetPartidoByIdQuery(id), cancellationToken);

    /// <summary>Programa un partido en el calendario del torneo. Requiere el encabezado Idempotency-Key.</summary>
    [HttpPost]
    [ProducesResponseType<PartidoDetalleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public async Task<IActionResult> Schedule(SchedulePartidoRequest request, CancellationToken cancellationToken) =>
        await CreatedAsync(
            await Dispatcher.Send(request.ToCommand(), cancellationToken),
            id => new GetPartidoByIdQuery(id),
            nameof(GetById),
            null,
            cancellationToken);

    /// <summary>Reprogramación completa (idempotente). Solo partidos en estado Programado.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<PartidoDetalleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> Update(Guid id, UpdatePartidoRequest request, CancellationToken cancellationToken) =>
        SendThenQueryAsync(request.ToCommand(id), new GetPartidoByIdQuery(id), cancellationToken);

    /// <summary>Modificación parcial: fecha, sede o cancelación (estado "Cancelado").</summary>
    [HttpPatch("{id:guid}")]
    [Consumes(MediaTypes.Json, MediaTypes.MergePatchJson)]
    [ProducesResponseType<PartidoDetalleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> Patch(Guid id, PatchPartidoRequest request, CancellationToken cancellationToken) =>
        SendThenQueryAsync(request.ToCommand(id), new GetPartidoByIdQuery(id), cancellationToken);

    /// <summary>Eliminación lógica idempotente. Un partido jugado no puede eliminarse.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        SendNoContentAsync(new DeletePartidoCommand(id), cancellationToken);

    /// <summary>
    /// Registra el resultado y los goleadores. Requiere el encabezado Idempotency-Key.
    /// Un partido que ya tiene resultado responde 409.
    /// </summary>
    [HttpPost("{id:guid}/resultado")]
    [ProducesResponseType<PartidoDetalleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public async Task<IActionResult> RegisterResult(Guid id, RegisterPartidoResultRequest request, CancellationToken cancellationToken)
    {
        var result = await Dispatcher.Send(request.ToCommand(id), cancellationToken);

        return await CreatedAsync(
            result.IsSuccess ? Result.Success(id) : Result.Failure<Guid>(result.Error),
            partidoId => new GetPartidoByIdQuery(partidoId),
            nameof(GetById),
            null,
            cancellationToken);
    }
}
