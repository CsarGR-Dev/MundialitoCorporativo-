using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MundialitoCorporativo.IntegrationTests.Api;

/// <summary>
/// Observabilidad y trazabilidad por HTTP (ítem 7): CorrelationId y TraceId en cada respuesta y en los errores,
/// y eventos de dominio registrados en EventosDominio enlazados al request que los originó.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class ObservabilitySqlServerTests(SqlServerDatabaseFixture fixture) : IDisposable
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string TraceHeader = "X-Trace-Id";

    private readonly MundialitoApiFactory? _factory = fixture.IsAvailable ? new MundialitoApiFactory(fixture.ConnectionString) : null;
    private HttpClient? _client;

    private HttpClient Client => _client ??= _factory!.CreateClient();

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [SkippableFact]
    public async Task Response_ShouldReturnReceivedCorrelationIdAndTraceId()
    {
        SkipIfUnavailable();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/posiciones-juego");
        request.Headers.Add(CorrelationHeader, "demo-observabilidad-1");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("demo-observabilidad-1", Header(response, CorrelationHeader));
        Assert.Matches("^[0-9a-f]{32}$", Header(response, TraceHeader));
    }

    [SkippableFact]
    public async Task Response_WithoutCorrelationId_ShouldGenerateOne()
    {
        SkipIfUnavailable();

        var response = await Client.GetAsync("/api/posiciones-juego");

        Assert.True(Guid.TryParse(Header(response, CorrelationHeader), out _));
    }

    [SkippableFact]
    public async Task Response_WithIncomingTraceparent_ShouldContinueThatTrace()
    {
        SkipIfUnavailable();
        const string traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/posiciones-juego");
        request.Headers.Add("traceparent", $"00-{traceId}-00f067aa0ba902b7-01");

        var response = await Client.SendAsync(request);

        Assert.Equal(traceId, Header(response, TraceHeader));
    }

    [SkippableFact]
    public async Task ErrorResponse_ShouldIncludeSameTraceIdAndCorrelationIdAsHeaders()
    {
        SkipIfUnavailable();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/equipos/{Guid.NewGuid()}");
        request.Headers.Add(CorrelationHeader, "demo-error-404");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Header(response, TraceHeader), problem.GetProperty("traceId").GetString());
        Assert.Equal("demo-error-404", problem.GetProperty("correlationId").GetString());
    }

    [SkippableFact]
    public async Task CreateEquipo_ShouldRegisterDomainEventLinkedToRequestTrace()
    {
        SkipIfUnavailable();
        var correlationId = $"demo-{Guid.NewGuid():N}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/equipos")
        {
            Content = JsonContent.Create(new { nombre = $"Equipo {Guid.NewGuid():N}"[..20], abreviatura = UniqueTestData.Abreviatura('O') })
        };
        request.Headers.Add(ApiClientExtensions.IdempotencyKeyHeader, Guid.NewGuid().ToString());
        request.Headers.Add(CorrelationHeader, correlationId);

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var equipoId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await using var context = fixture.CreateContext();
        var registro = await context.EventosDominio.SingleAsync(evento => evento.AgregadoId == equipoId);
        Assert.Equal("EquipoCreatedDomainEvent", registro.TipoEvento);
        Assert.Equal(Header(response, TraceHeader), registro.TraceId);
        Assert.Equal(correlationId, registro.CorrelationId);
        Assert.NotNull(registro.FechaProcesado);
    }

    [SkippableFact]
    public async Task UnexpectedError_ShouldReturn500WithoutInternalDetailsAndWithTrace()
    {
        SkipIfUnavailable();
        var unreachable = new SqlConnectionStringBuilder(fixture.ConnectionString)
        {
            InitialCatalog = $"Mundialito_NoExiste_{Guid.NewGuid():N}",
            ConnectTimeout = 5
        }.ConnectionString;
        using var factory = new MundialitoApiFactory(unreachable);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/equipos");
        request.Headers.Add(CorrelationHeader, "demo-error-500");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("demo-error-500", Header(response, CorrelationHeader));

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("Server.Unexpected", problem.GetProperty("errorCode").GetString());
        Assert.Equal(Header(response, TraceHeader), problem.GetProperty("traceId").GetString());
        Assert.Equal("demo-error-500", problem.GetProperty("correlationId").GetString());
        Assert.DoesNotContain("Mundialito_NoExiste", body, StringComparison.Ordinal);
        Assert.DoesNotContain("SqlException", body, StringComparison.Ordinal);
    }

    private static string Header(HttpResponseMessage response, string name) =>
        Assert.Single(response.Headers.GetValues(name));

    private void SkipIfUnavailable() => Skip.IfNot(fixture.IsAvailable, fixture.UnavailableReason);
}
