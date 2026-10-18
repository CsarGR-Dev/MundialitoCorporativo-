using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using MundialitoCorporativo.Api.Http;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Api;

public static class DependencyInjection
{
    /// <summary>
    /// Registra la capa HTTP: controladores, JSON, errores en ProblemDetails y manejo global de excepciones.
    /// </summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services
            .AddControllers(options =>
            {
                // Los campos obligatorios los valida FluentValidation en Application, con mensajes en español.
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                options.ModelMetadataDetailsProviders.Add(new PagedQueryBindingMetadataProvider());
            })
            .AddJsonOptions(options =>
            {
                ConfigureJson(options.JsonSerializerOptions);

                // Un JSON inválido responde 400 sin exponer nombres de tipos internos.
                options.AllowInputFormatterExceptionMessages = false;
            })
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = CreateInvalidModelStateResponse);

        // PATCH acepta JSON Merge Patch (RFC 7396) además de application/json.
        services.PostConfigure<MvcOptions>(options =>
            options.InputFormatters.OfType<SystemTextJsonInputFormatter>().First().SupportedMediaTypes.Add(MediaTypes.MergePatchJson));

        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));
        services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblemDetails.AddTraceInformation);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // GlobalExceptionHandler ya registra la excepción; se evita el log duplicado del middleware del framework.
        services.Configure<ExceptionHandlerOptions>(options =>
            options.SuppressDiagnosticsCallback = context => context.ExceptionHandledBy == ExceptionHandledType.ExceptionHandlerService);
        services.AddRouting(options => options.LowercaseUrls = true);
        services.AddOpenApi(options => options.AddOperationTransformer(RemoveNonBindableParameters));

        return services;
    }

    /// <summary>
    /// Quita del documento OpenAPI los parámetros que no se leen de la petición
    /// (propiedades calculadas de las Queries y valores que asigna la ruta).
    /// </summary>
    private static Task RemoveNonBindableParameters(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var hidden = context.Description.ParameterDescriptions
            .Where(parameter => parameter.ModelMetadata is { IsBindingAllowed: false })
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (hidden.Count > 0 && operation.Parameters is { } parameters)
        {
            foreach (var parameter in parameters.Where(parameter => parameter.Name is not null && hidden.Contains(parameter.Name)).ToList())
            {
                parameters.Remove(parameter);
            }
        }

        return Task.CompletedTask;
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        // Estados como texto ("Jugado", "EnCurso") y acentos legibles en el JSON ("Tecnología" se envía tal cual, sin escapar).
        options.Converters.Add(new JsonStringEnumConverter());
        options.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    }

    /// <summary>
    /// JSON mal formado o tipos inválidos (por ejemplo, texto en pageNumber): 400 con el mismo formato
    /// que las validaciones de Application.
    /// </summary>
    private static IActionResult CreateInvalidModelStateResponse(ActionContext context)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(
            context.HttpContext,
            context.ModelState,
            StatusCodes.Status400BadRequest,
            detail: "La petición no tiene el formato esperado.",
            instance: context.HttpContext.Request.Path.Value);

        problem.Extensions[ApiProblemDetails.ErrorCodeKey] = ValidationError.DefaultCode;

        return new BadRequestObjectResult(problem) { ContentTypes = { MediaTypes.ProblemJson } };
    }
}
