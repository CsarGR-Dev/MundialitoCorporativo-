using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MundialitoCorporativo.Api.Observability;
using MundialitoCorporativo.Application.Common.Observability;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Api.Http;

/// <summary>
/// Construye las respuestas de error en formato ProblemDetails (RFC 9457) a partir de un <see cref="Error"/>.
/// Todas incluyen <c>traceId</c>, <c>correlationId</c> y, cuando existe, el <c>errorCode</c> del Result Pattern.
/// </summary>
public static class ApiProblemDetails
{
    public const string ErrorCodeKey = "errorCode";
    public const string TraceIdKey = "traceId";
    public const string CorrelationIdKey = "correlationId";

    public static readonly Error UnexpectedError =
        Error.Failure("Ocurrió un error inesperado. Intente de nuevo más tarde.", "Server.Unexpected");

    public static ProblemDetails Create(HttpContext httpContext, Error error)
    {
        var factory = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var statusCode = ErrorHttpMapping.ToStatusCode(error.Type);
        var instance = httpContext.Request.Path.Value;

        ProblemDetails problem;
        if (error is ValidationError validationError)
        {
            var modelState = new ModelStateDictionary();
            foreach (var fieldError in validationError.Errors)
            {
                modelState.AddModelError(fieldError.Field, fieldError.Message);
            }

            problem = factory.CreateValidationProblemDetails(httpContext, modelState, statusCode, detail: error.Message, instance: instance);
        }
        else
        {
            problem = factory.CreateProblemDetails(httpContext, statusCode, detail: error.Message, instance: instance);
        }

        if (error.Code is not null)
        {
            problem.Extensions[ErrorCodeKey] = error.Code;
        }

        return problem;
    }

    /// <summary>
    /// Se aplica a todo ProblemDetails de la API (errores de negocio, validación de formato, 404 de ruta y 500).
    /// El traceId es el mismo del header X-Trace-Id y de los logs, para ubicar el request a partir del error.
    /// </summary>
    public static void AddTraceInformation(ProblemDetailsContext context)
    {
        var extensions = context.ProblemDetails.Extensions;
        extensions[TraceIdKey] = ApplicationDiagnostics.CurrentTraceId ?? context.HttpContext.TraceIdentifier;

        if (CorrelationIdMiddleware.GetCorrelationId(context.HttpContext) is { } correlationId)
        {
            extensions[CorrelationIdKey] = correlationId;
        }
    }

    /// <summary>Escribe el error directamente en la respuesta (middleware y manejador de excepciones).</summary>
    public static async Task WriteAsync(HttpContext httpContext, Error error, Exception? exception = null)
    {
        var problem = Create(httpContext, error);
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
