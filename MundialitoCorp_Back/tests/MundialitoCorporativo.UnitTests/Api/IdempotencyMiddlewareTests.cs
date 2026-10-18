using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MundialitoCorporativo.Api;
using MundialitoCorporativo.Api.Idempotency;
using MundialitoCorporativo.Application.Abstractions.Idempotency;

namespace MundialitoCorporativo.UnitTests.Api;

/// <summary>
/// Idempotencia en POST (requerimiento ítems 6 y 15): clave obligatoria, persistencia, sin duplicados
/// y la misma respuesta original ante la repetición.
/// </summary>
public class IdempotencyMiddlewareTests
{
    private const string EquiposPath = "/api/equipos";
    private const string Body = """{"nombre":"Tecnología FC","abreviatura":"TEC"}""";

    private static readonly IServiceProvider Services = new ServiceCollection()
        .AddLogging()
        .AddPresentation()
        .BuildServiceProvider();

    private readonly InMemoryIdempotencyStore _store = new();
    private int _executions;

    [Fact]
    public async Task Post_WithoutKey_ShouldReturn400AndNotExecute()
    {
        var context = CreatePost(Body, key: null);

        await InvokeAsync(context, CreatedAction);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("Idempotency.KeyRequired", ReadErrorCode(context));
        Assert.Equal(0, _executions);
    }

    [Theory]
    [InlineData("clave con espacios")]
    [InlineData("claveñ")]
    public async Task Post_WithInvalidKey_ShouldReturn400(string key)
    {
        var context = CreatePost(Body, key);

        await InvokeAsync(context, CreatedAction);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("Idempotency.KeyInvalid", ReadErrorCode(context));
        Assert.Equal(0, _executions);
    }

    [Fact]
    public async Task Post_WithKeyLongerThanLimit_ShouldReturn400()
    {
        var context = CreatePost(Body, new string('k', IdempotencyMiddleware.MaxKeyLength + 1));

        await InvokeAsync(context, CreatedAction);

        Assert.Equal("Idempotency.KeyInvalid", ReadErrorCode(context));
    }

    [Fact]
    public async Task FirstPost_ShouldExecuteAndPersistOriginalResponse()
    {
        var context = CreatePost(Body, "clave-1");

        await InvokeAsync(context, CreatedAction);

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        Assert.Equal(1, _executions);
        Assert.False(context.Response.Headers.ContainsKey(IdempotencyMiddleware.ReplayedHeaderName));

        var record = Assert.Single(_store.Records);
        Assert.Equal(EstadoRegistroIdempotencia.Completado, record.Estado);
        Assert.Equal(StatusCodes.Status201Created, record.CodigoEstadoRespuesta);
        Assert.Equal(ReadBody(context), record.CuerpoRespuesta);
        Assert.Equal(EquiposPath, record.RutaSolicitud);
        Assert.Equal(64, record.HashSolicitud.Length);
    }

    [Fact]
    public async Task RepeatedPost_WithSameKeyAndBody_ShouldReturnOriginalResponseWithoutExecutingAgain()
    {
        var first = CreatePost(Body, "clave-1");
        await InvokeAsync(first, CreatedAction);

        var second = CreatePost(Body, "clave-1");
        await InvokeAsync(second, CreatedAction);

        Assert.Equal(1, _executions);
        Assert.Equal(first.Response.StatusCode, second.Response.StatusCode);
        Assert.Equal(ReadBody(first), ReadBody(second));
        Assert.Equal(first.Response.Headers.Location.ToString(), second.Response.Headers.Location.ToString());
        Assert.Equal(first.Response.ContentType, second.Response.ContentType);
        Assert.Equal("true", second.Response.Headers[IdempotencyMiddleware.ReplayedHeaderName].ToString());
    }

    [Fact]
    public async Task RepeatedPost_WithSameJsonInDifferentFormat_ShouldBeTreatedAsSameRequest()
    {
        await InvokeAsync(CreatePost(Body, "clave-1"), CreatedAction);

        var formatted = CreatePost("""
            {
              "nombre": "Tecnología FC",
              "abreviatura": "TEC"
            }
            """, "clave-1");
        await InvokeAsync(formatted, CreatedAction);

        Assert.Equal(1, _executions);
        Assert.Equal(StatusCodes.Status201Created, formatted.Response.StatusCode);
    }

    [Fact]
    public async Task RepeatedPost_WithSameKeyAndDifferentBody_ShouldReturn409()
    {
        await InvokeAsync(CreatePost(Body, "clave-1"), CreatedAction);

        var context = CreatePost("""{"nombre":"Otro equipo","abreviatura":"OTR"}""", "clave-1");
        await InvokeAsync(context, CreatedAction);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("Idempotency.KeyReused", ReadErrorCode(context));
        Assert.Equal(1, _executions);
    }

    [Fact]
    public async Task Post_WhileSameKeyIsInProgress_ShouldReturn409()
    {
        // Primera petición reservada y todavía sin terminar.
        var inProgress = CreatePost(Body, "clave-1");
        var hash = await IdempotencyMiddleware.ComputeRequestHashAsync(inProgress.Request, CancellationToken.None);
        _store.Add(new RegistroIdempotencia
        {
            ClaveIdempotencia = "clave-1",
            RutaSolicitud = EquiposPath,
            MetodoSolicitud = HttpMethods.Post,
            HashSolicitud = hash,
            Estado = EstadoRegistroIdempotencia.EnProceso
        });

        var context = CreatePost(Body, "clave-1");
        await InvokeAsync(context, CreatedAction);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("Idempotency.InProgress", ReadErrorCode(context));
        Assert.Equal(0, _executions);
    }

    [Fact]
    public async Task SameKey_OnDifferentRoutes_ShouldBeIndependent()
    {
        await InvokeAsync(CreatePost(Body, "clave-1", "/api/equipos"), CreatedAction);
        await InvokeAsync(CreatePost(Body, "clave-1", "/api/torneos"), CreatedAction);

        Assert.Equal(2, _executions);
        Assert.Equal(2, _store.Records.Count);
    }

    [Fact]
    public async Task ClientErrorResponse_ShouldBeStoredAndReplayed()
    {
        static async Task ConflictAction(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsync("""{"errorCode":"Equipo.DuplicateNombre"}""");
        }

        await InvokeAsync(CreatePost(Body, "clave-1"), ConflictAction);
        var repeated = CreatePost(Body, "clave-1");
        await InvokeAsync(repeated, CreatedAction);

        Assert.Equal(StatusCodes.Status409Conflict, repeated.Response.StatusCode);
        Assert.Equal("Equipo.DuplicateNombre", ReadErrorCode(repeated));
        Assert.Equal(0, _executions);
    }

    [Fact]
    public async Task ServerErrorResponse_ShouldReleaseKeySoTheClientCanRetry()
    {
        static Task FailingAction(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Task.CompletedTask;
        }

        await InvokeAsync(CreatePost(Body, "clave-1"), FailingAction);
        Assert.Empty(_store.Records);

        var retry = CreatePost(Body, "clave-1");
        await InvokeAsync(retry, CreatedAction);

        Assert.Equal(StatusCodes.Status201Created, retry.Response.StatusCode);
        Assert.Equal(1, _executions);
    }

    [Fact]
    public async Task UnhandledException_ShouldReleaseKeyAndPropagate()
    {
        static Task ThrowingAction(HttpContext context) => throw new InvalidOperationException("Fallo inesperado");

        await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeAsync(CreatePost(Body, "clave-1"), ThrowingAction));

        Assert.Empty(_store.Records);
    }

    [Fact]
    public async Task RequestBody_ShouldRemainReadableByTheAction()
    {
        string? bodySeenByAction = null;
        async Task ReadingAction(HttpContext context)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            bodySeenByAction = await reader.ReadToEndAsync();
            context.Response.StatusCode = StatusCodes.Status201Created;
        }

        await InvokeAsync(CreatePost(Body, "clave-1"), ReadingAction);

        Assert.Equal(Body, bodySeenByAction);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task OtherMethods_ShouldNotRequireKey(string method)
    {
        var context = CreatePost(Body, key: null);
        context.Request.Method = method;

        await InvokeAsync(context, CreatedAction);

        Assert.Equal(1, _executions);
        Assert.Empty(_store.Records);
    }

    [Fact]
    public async Task PostToUnknownRoute_ShouldNotRequireKey()
    {
        var context = CreatePost(Body, key: null);
        context.SetEndpoint(null);

        await InvokeAsync(context, CreatedAction);

        Assert.Equal(1, _executions);
    }

    private async Task CreatedAction(HttpContext context)
    {
        var execution = Interlocked.Increment(ref _executions);
        context.Response.StatusCode = StatusCodes.Status201Created;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.Location = $"http://localhost/api/equipos/{execution}";
        await context.Response.WriteAsync($$"""{"id":"{{execution}}","nombre":"Tecnología FC"}""");
    }

    private Task InvokeAsync(HttpContext context, RequestDelegate action) =>
        new IdempotencyMiddleware(action, NullLogger<IdempotencyMiddleware>.Instance).InvokeAsync(context, _store);

    private static DefaultHttpContext CreatePost(string body, string? key, string path = EquiposPath)
    {
        var context = new DefaultHttpContext { RequestServices = Services };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "test"));

        if (key is not null)
        {
            context.Request.Headers[IdempotencyMiddleware.HeaderName] = key;
        }

        return context;
    }

    private static string ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        return reader.ReadToEnd();
    }

    private static string? ReadErrorCode(HttpContext context)
    {
        using var document = JsonDocument.Parse(ReadBody(context));
        return document.RootElement.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }
}
