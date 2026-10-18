using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MundialitoCorporativo.Api;
using MundialitoCorporativo.Api.Http;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;

namespace MundialitoCorporativo.UnitTests.Api;

/// <summary>
/// Traducción del Result Pattern a HTTP (requerimiento ítem 5): 400, 404, 409 y errores en ProblemDetails.
/// </summary>
public class ErrorHttpMappingTests
{
    private static readonly IServiceProvider Services = new ServiceCollection()
        .AddLogging()
        .AddPresentation()
        .BuildServiceProvider();

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    [InlineData(ErrorType.None, StatusCodes.Status500InternalServerError)]
    public void ToStatusCode_ShouldMapEveryErrorType(ErrorType type, int expectedStatusCode)
    {
        Assert.Equal(expectedStatusCode, ErrorHttpMapping.ToStatusCode(type));
    }

    [Fact]
    public void Create_ForBusinessError_ShouldIncludeMessageCodeAndTraceId()
    {
        var context = CreateContext("/api/equipos");

        var problem = ApiProblemDetails.Create(context, EquipoErrors.DuplicateNombre("Tecnología FC"));

        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Ya existe un equipo con el nombre 'Tecnología FC'.", problem.Detail);
        Assert.Equal("Equipo.DuplicateNombre", problem.Extensions[ApiProblemDetails.ErrorCodeKey]);
        Assert.Equal("/api/equipos", problem.Instance);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public void Create_ForValidationError_ShouldReturnErrorsByField()
    {
        var context = CreateContext("/api/equipos");
        var error = new ValidationError(
        [
            new FieldError("nombre", "El nombre es obligatorio."),
            new FieldError("nombre", "El nombre debe tener al menos 3 caracteres."),
            new FieldError("abreviatura", "La abreviatura es obligatoria.")
        ]);

        var problem = Assert.IsType<ValidationProblemDetails>(ApiProblemDetails.Create(context, error));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal(ValidationError.DefaultCode, problem.Extensions[ApiProblemDetails.ErrorCodeKey]);
        Assert.Equal(2, problem.Errors["nombre"].Length);
        Assert.Single(problem.Errors["abreviatura"]);
    }

    [Fact]
    public void Create_ForErrorWithoutCode_ShouldOmitErrorCode()
    {
        var problem = ApiProblemDetails.Create(CreateContext("/api/partidos"), Error.NotFound("No existe."));

        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.False(problem.Extensions.ContainsKey(ApiProblemDetails.ErrorCodeKey));
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext { RequestServices = Services };
        context.Request.Path = path;
        return context;
    }
}
