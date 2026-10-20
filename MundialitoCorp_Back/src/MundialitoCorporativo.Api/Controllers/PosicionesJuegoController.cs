using MediatR;
using Microsoft.AspNetCore.Mvc;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.PosicionesJuego;
using MundialitoCorporativo.Application.PosicionesJuego.Queries;

namespace MundialitoCorporativo.Api.Controllers;

/// <summary>
/// Catálogo de posiciones de juego (solo lectura) para registrar integrantes.
/// </summary>
[Route("api/posiciones-juego")]
public sealed class PosicionesJuegoController(ISender sender) : ApiController(sender)
{
    /// <summary>Lista paginada. Filtro: activo.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<PosicionJuegoResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromQuery] GetPosicionesJuegoQuery query, CancellationToken cancellationToken) =>
        QueryAsync(query, cancellationToken);
}
