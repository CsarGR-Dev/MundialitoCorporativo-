using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using MundialitoCorporativo.Api.Observability;

namespace MundialitoCorporativo.UnitTests.Api;

/// <summary>
/// Registro de la duración de cada request (ítem 7) con el nivel que corresponde al resultado.
/// </summary>
public class RequestTimingMiddlewareTests
{
    private readonly FakeLogger<RequestTimingMiddleware> _logger = new();

    [Theory]
    [InlineData(StatusCodes.Status200OK, LogLevel.Information)]
    [InlineData(StatusCodes.Status201Created, LogLevel.Information)]
    [InlineData(StatusCodes.Status204NoContent, LogLevel.Information)]
    [InlineData(StatusCodes.Status400BadRequest, LogLevel.Warning)]
    [InlineData(StatusCodes.Status404NotFound, LogLevel.Warning)]
    [InlineData(StatusCodes.Status409Conflict, LogLevel.Warning)]
    [InlineData(StatusCodes.Status500InternalServerError, LogLevel.Error)]
    public async Task Request_ShouldLogDurationWithLevelByStatusCode(int statusCode, LogLevel expectedLevel)
    {
        var context = CreateContext("/api/equipos");

        await InvokeAsync(context, TimeSpan.FromSeconds(30), httpContext =>
        {
            httpContext.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });

        var record = _logger.LatestRecord;
        Assert.Equal(1, _logger.Collector.Count);
        Assert.Equal(expectedLevel, record.Level);
        Assert.Equal("GET", record.GetStructuredStateValue("RequestMethod"));
        Assert.Equal("/api/equipos", record.GetStructuredStateValue("RequestPath"));
        Assert.Equal(statusCode.ToString(System.Globalization.CultureInfo.InvariantCulture), record.GetStructuredStateValue("StatusCode"));
        Assert.NotNull(record.GetStructuredStateValue("ElapsedMs"));
    }

    [Fact]
    public async Task SlowRequest_ShouldLogWarning()
    {
        var context = CreateContext("/api/torneos");

        await InvokeAsync(context, TimeSpan.FromMilliseconds(1), _ => Task.Delay(50));

        Assert.Equal(LogLevel.Warning, _logger.LatestRecord.Level);
        Assert.Equal("1", _logger.LatestRecord.GetStructuredStateValue("ThresholdMs"));
    }

    [Fact]
    public async Task Request_ShouldNotLogQueryString()
    {
        var context = CreateContext("/api/jugadores");
        context.Request.QueryString = new QueryString("?nombre=Ana");

        await InvokeAsync(context, TimeSpan.FromSeconds(30), _ => Task.CompletedTask);

        Assert.DoesNotContain("Ana", _logger.LatestRecord.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnhandledException_ShouldBeLoggedAsErrorAndRethrown()
    {
        var context = CreateContext("/api/equipos");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeAsync(context, TimeSpan.FromSeconds(30), _ => throw new InvalidOperationException("Fallo")));

        Assert.Equal(LogLevel.Error, _logger.LatestRecord.Level);
        Assert.IsType<InvalidOperationException>(_logger.LatestRecord.Exception);
    }

    private Task InvokeAsync(HttpContext context, TimeSpan slowThreshold, RequestDelegate next)
    {
        var options = Options.Create(new ObservabilityOptions { SlowRequestThreshold = slowThreshold });
        return new RequestTimingMiddleware(next, options, _logger).InvokeAsync(context);
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        return context;
    }
}
