using MediatR;
using Microsoft.AspNetCore.Mvc;
using MundialitoCorporativo.Api.Contracts;
using MundialitoCorporativo.Api.Http;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Partidos;
using MundialitoCorporativo.Application.Partidos.Queries;
using MundialitoCorporativo.Application.Torneos;
using MundialitoCorporativo.Application.Torneos.Commands;
using MundialitoCorporativo.Application.Torneos.Queries;

namespace MundialitoCorporativo.Api.Controllers;

/// <summary>
/// Torneos: datos, equipos participantes, calendario, tabla de posiciones y goleadores.
/// </summary>
[Route("api/torneos")]
public sealed class TorneosController(ISender sender) : ApiController(sender)
{
    /// <summary>Lista paginada. Filtros: nombre, estado.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<TorneoResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] GetTorneosQuery query, CancellationToken cancellationToken) =>
        QueryAsync(query, cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TorneoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        QueryAsync(new GetTorneoByIdQuery(id), cancellationToken);

    /// <summary>Crea un torneo en estado Planificado. Requiere el encabezado Idempotency-Key.</summary>
    [HttpPost]
    [ProducesResponseType<TorneoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public async Task<IActionResult> Create(TorneoRequest request, CancellationToken cancellationToken) =>
        await CreatedAsync(
            await Sender.Send(request.ToCreateCommand(), cancellationToken),
            id => new GetTorneoByIdQuery(id),
            nameof(GetById),
            null,
            cancellationToken);

    /// <summary>Reemplazo completo (idempotente).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<TorneoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> Update(Guid id, TorneoRequest request, CancellationToken cancellationToken) =>
        SendThenQueryAsync(request.ToUpdateCommand(id), new GetTorneoByIdQuery(id), cancellationToken);

    /// <summary>Modificación parcial de datos o cambio de estado.</summary>
    [HttpPatch("{id:guid}")]
    [Consumes(MediaTypes.Json, MediaTypes.MergePatchJson)]
    [ProducesResponseType<TorneoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> Patch(Guid id, PatchTorneoRequest request, CancellationToken cancellationToken) =>
        SendThenQueryAsync(request.ToCommand(id), new GetTorneoByIdQuery(id), cancellationToken);

    /// <summary>Eliminación lógica idempotente. No se elimina un torneo con partidos jugados.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        SendNoContentAsync(new DeleteTorneoCommand(id), cancellationToken);

    /// <summary>Equipos participantes del torneo, paginados. Filtro: nombre.</summary>
    [HttpGet("{id:guid}/equipos")]
    [ProducesResponseType<PagedResponse<TorneoEquipoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    public Task<IActionResult> GetEquipos(Guid id, [FromQuery] GetTorneoEquiposQuery query, CancellationToken cancellationToken) =>
        QueryAsync(query with { TorneoId = id }, cancellationToken);

    /// <summary>Inscribe un equipo en el torneo. Requiere el encabezado Idempotency-Key.</summary>
    [HttpPost("{id:guid}/equipos")]
    [ProducesResponseType<InscripcionEquipoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public async Task<IActionResult> RegisterEquipo(Guid id, RegisterEquipoInTorneoRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(request.ToCommand(id), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetEquipos), new { id }, new InscripcionEquipoResponse(id, request.EquipoId))
            : ErrorResult(result.Error);
    }

    /// <summary>Retira un equipo del torneo (solo si no tiene partidos en él).</summary>
    [HttpDelete("{id:guid}/equipos/{equipoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypes.ProblemJson)]
    public Task<IActionResult> WithdrawEquipo(Guid id, Guid equipoId, CancellationToken cancellationToken) =>
        SendNoContentAsync(new WithdrawEquipoFromTorneoCommand(id, equipoId), cancellationToken);

    /// <summary>Calendario del torneo. Filtros: fechaDesde, fechaHasta, equipoId, estado.</summary>
    [HttpGet("{id:guid}/partidos")]
    [ProducesResponseType<PagedResponse<PartidoResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetPartidos(Guid id, [FromQuery] GetPartidosQuery query, CancellationToken cancellationToken) =>
        QueryAsync(query with { TorneoId = id }, cancellationToken);

    /// <summary>
    /// Tabla de posiciones calculada a partir de los partidos jugados.
    /// Orden oficial: puntos, diferencia de gol y goles a favor. Filtro: equipo.
    /// </summary>
    [HttpGet("{id:guid}/tabla-posiciones")]
    [ProducesResponseType<PagedResponse<FilaTablaPosicionesResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    public Task<IActionResult> GetTablaPosiciones(Guid id, [FromQuery] GetTablaPosicionesQuery query, CancellationToken cancellationToken) =>
        QueryAsync(query with { TorneoId = id }, cancellationToken);

    /// <summary>Goleadores del torneo (goles en partidos jugados). Filtros: equipoId, jugador.</summary>
    [HttpGet("{id:guid}/goleadores")]
    [ProducesResponseType<PagedResponse<GoleadorResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypes.ProblemJson)]
    public Task<IActionResult> GetGoleadores(Guid id, [FromQuery] GetGoleadoresQuery query, CancellationToken cancellationToken) =>
        QueryAsync(query with { TorneoId = id }, cancellationToken);
}
