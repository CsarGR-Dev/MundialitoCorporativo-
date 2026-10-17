using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MundialitoCorporativo.IntegrationTests.Api;

/// <summary>
/// Levanta la API completa en memoria (pipeline HTTP, middleware, controladores) contra la base temporal.
/// </summary>
internal sealed class MundialitoApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);

        // Varias instancias de la API corren en el mismo proceso: sin archivo de log compartido.
        builder.UseSetting("Observability:LogFilePath", "");
    }
}

internal static class ApiClientExtensions
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    public static Task<HttpResponseMessage> PostWithKeyAsync(this HttpClient client, string path, object body, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add(IdempotencyKeyHeader, key ?? Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> PostRawWithKeyAsync(this HttpClient client, string path, string json, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add(IdempotencyKeyHeader, key);
        return client.SendAsync(request);
    }
}
