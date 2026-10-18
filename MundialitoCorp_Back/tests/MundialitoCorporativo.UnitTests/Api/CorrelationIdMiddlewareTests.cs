using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using MundialitoCorporativo.Api.Observability;
using MundialitoCorporativo.Application.Common.Observability;

namespace MundialitoCorporativo.UnitTests.Api;

/// <summary>
/// Trazabilidad por request (ítem 7): CorrelationId por request y TraceId en la respuesta.
/// </summary>
public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Request_WithValidCorrelationId_ShouldKeepItAndReturnIt()
    {
        var (context, response) = CreateContext("pedido-123");

        await InvokeAsync(context);
        await response.StartAsync();

        Assert.Equal("pedido-123", CorrelationIdMiddleware.GetCorrelationId(context));
        Assert.Equal("pedido-123", context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("valor con espacios")]
    [InlineData("correlación")]
    public async Task Request_WithoutValidCorrelationId_ShouldGenerateNewGuid(string? received)
    {
        var (context, response) = CreateContext(received);

        await InvokeAsync(context);
        await response.StartAsync();

        var correlationId = CorrelationIdMiddleware.GetCorrelationId(context);
        Assert.True(Guid.TryParse(correlationId, out _));
        Assert.Equal(correlationId, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task Request_WithCorrelationIdLongerThanLimit_ShouldGenerateNewOne()
    {
        var (context, _) = CreateContext(new string('c', CorrelationIdMiddleware.MaxLength + 1));

        await InvokeAsync(context);

        Assert.True(Guid.TryParse(CorrelationIdMiddleware.GetCorrelationId(context), out _));
    }

    [Fact]
    public async Task Request_ShouldReturnTraceIdAndShareCorrelationIdWithInnerLayers()
    {
        using var request = new Activity("GET /api/equipos").SetIdFormat(ActivityIdFormat.W3C).Start();
        var (context, response) = CreateContext("pedido-456");
        string? correlationSeenInside = null;

        await InvokeAsync(context, _ =>
        {
            correlationSeenInside = ApplicationDiagnostics.CurrentCorrelationId;
            return Task.CompletedTask;
        });
        await response.StartAsync();

        Assert.Equal("pedido-456", correlationSeenInside);
        Assert.Equal(request.TraceId.ToHexString(), context.Response.Headers[CorrelationIdMiddleware.TraceIdHeaderName].ToString());
    }

    private static Task InvokeAsync(HttpContext context, RequestDelegate? next = null) =>
        new CorrelationIdMiddleware(next ?? (_ => Task.CompletedTask), NullLogger<CorrelationIdMiddleware>.Instance).InvokeAsync(context);

    private static (DefaultHttpContext Context, StartableResponseFeature Response) CreateContext(string? correlationId)
    {
        var context = new DefaultHttpContext();
        var response = new StartableResponseFeature();
        context.Features.Set<IHttpResponseFeature>(response);

        if (correlationId is not null)
        {
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        }

        return (context, response);
    }

    /// <summary>Simula el inicio de la respuesta para ejecutar los callbacks de OnStarting, como hace Kestrel.</summary>
    private sealed class StartableResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _callbacks = [];

        public override void OnStarting(Func<object, Task> callback, object state) => _callbacks.Add((callback, state));

        public async Task StartAsync()
        {
            foreach (var (callback, state) in _callbacks)
            {
                await callback(state);
            }
        }
    }
}
