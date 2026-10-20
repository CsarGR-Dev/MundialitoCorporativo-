using MediatR;
using Microsoft.AspNetCore.Mvc;
using MundialitoCorporativo.Api.Http;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Api.Controllers;

/// <summary>
/// Base de los controladores: solo orquestan. Envían Commands o Queries con MediatR (ISender) y traducen el Result a HTTP.
/// </summary>
[ApiController]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypes.ProblemJson)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypes.ProblemJson)]
public abstract class ApiController(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;

    /// <summary>GET: 200 con el resultado, o el error correspondiente (400/404).</summary>
    protected async Task<IActionResult> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : ErrorResult(result.Error);
    }

    /// <summary>
    /// POST de creación: 201 con el recurso creado (leído por su Query) y el encabezado Location.
    /// </summary>
    protected async Task<IActionResult> CreatedAsync<TResponse>(
        Result<Guid> creation,
        Func<Guid, IQuery<TResponse>> resourceQuery,
        string actionName,
        string? controllerName,
        CancellationToken cancellationToken)
    {
        if (creation.IsFailure)
        {
            return ErrorResult(creation.Error);
        }

        var resource = await Sender.Send(resourceQuery(creation.Value), cancellationToken);

        return resource.IsSuccess
            ? CreatedAtAction(actionName, controllerName, new { id = creation.Value }, resource.Value)
            : ErrorResult(resource.Error);
    }

    /// <summary>PUT y PATCH: ejecuta el Command y responde 200 con el recurso actualizado.</summary>
    protected async Task<IActionResult> SendThenQueryAsync<TResponse>(
        ICommand command,
        IQuery<TResponse> resourceQuery,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess ? await QueryAsync(resourceQuery, cancellationToken) : ErrorResult(result.Error);
    }

    /// <summary>DELETE: 204 sin cuerpo, también cuando el recurso ya estaba eliminado (idempotente).</summary>
    protected async Task<IActionResult> SendNoContentAsync(ICommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : ErrorResult(result.Error);
    }

    /// <summary>Respuesta de error en ProblemDetails: 400, 404, 409 o 500 según el tipo de error.</summary>
    protected ObjectResult ErrorResult(Error error)
    {
        var problem = ApiProblemDetails.Create(HttpContext, error);

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { MediaTypes.ProblemJson }
        };
    }
}
