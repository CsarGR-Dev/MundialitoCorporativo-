using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Net.Http.Headers;
using MundialitoCorporativo.Api.Http;
using MundialitoCorporativo.Application.Abstractions.Idempotency;

namespace MundialitoCorporativo.Api.Idempotency;

/// <summary>
/// Idempotencia en POST (requerimiento ítem 6): exige el encabezado Idempotency-Key, lo persiste y,
/// si la petición se repite, devuelve la misma respuesta original sin volver a ejecutar el Command.
/// </summary>
/// <remarks>
/// Flujo: reservar la clave (EnProceso) → ejecutar → guardar código, cuerpo y encabezados (Completado).
/// Misma clave y mismo cuerpo → respuesta original + "Idempotent-Replayed: true".
/// Misma clave con otro cuerpo → 409. Clave aún en proceso → 409.
/// Una respuesta 5xx o una excepción liberan la clave para que el cliente pueda reintentar.
/// </remarks>
public sealed partial class IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayedHeaderName = "Idempotent-Replayed";
    public const int MaxKeyLength = 100;

    private static readonly string[] ReplayedHeaders = [HeaderNames.Location, HeaderNames.ContentType];

    public async Task InvokeAsync(HttpContext context, IIdempotencyStore store)
    {
        // Solo los POST que llegan a un endpoint; una ruta inexistente responde 404 sin pedir la clave.
        if (!HttpMethods.IsPost(context.Request.Method) || context.GetEndpoint() is null)
        {
            await next(context);
            return;
        }

        var keyValues = context.Request.Headers[HeaderName];
        if (keyValues.Count == 0)
        {
            await ApiProblemDetails.WriteAsync(context, IdempotencyErrors.KeyRequired);
            return;
        }

        var key = keyValues.Count == 1 ? keyValues[0] : null;
        if (!IsValidKey(key))
        {
            await ApiProblemDetails.WriteAsync(context, IdempotencyErrors.KeyInvalid);
            return;
        }

        var path = NormalizePath(context.Request.Path);
        var requestHash = await ComputeRequestHashAsync(context.Request, context.RequestAborted);

        var existing = await store.TryBeginAsync(
            new IdempotencyRequest(key!, path, HttpMethods.Post, requestHash), context.RequestAborted);

        if (existing is null)
        {
            await ExecuteAndStoreAsync(context, store, key!, path);
        }
        else
        {
            await RespondToRepeatedRequestAsync(context, existing, path, requestHash);
        }
    }

    private async Task ExecuteAndStoreAsync(HttpContext context, IIdempotencyStore store, string key, string path)
    {
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);
        }
        catch
        {
            // La operación no terminó: se libera la clave y el manejador global responde 500.
            context.Response.Body = originalBody;
            await store.ReleaseAsync(key, path, CancellationToken.None);
            throw;
        }

        context.Response.Body = originalBody;
        var statusCode = context.Response.StatusCode;

        // CancellationToken.None: aunque el cliente se desconecte, la respuesta debe quedar guardada.
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            await store.ReleaseAsync(key, path, CancellationToken.None);
            LogKeyReleased(logger, path, statusCode);
        }
        else
        {
            var response = new IdempotencyResponse(
                statusCode,
                Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length),
                SerializeHeaders(context.Response.Headers));

            try
            {
                await store.CompleteAsync(key, path, response, CancellationToken.None);
                LogResponseStored(logger, path, statusCode);
            }
            catch (Exception exception)
            {
                // La operación ya se confirmó: se entrega su respuesta aunque no se haya podido guardar.
                // La reserva queda EnProceso y vence según Idempotency:InProgressTimeout.
                LogStoreFailed(logger, exception, path);
            }
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(originalBody, context.RequestAborted);
    }

    private async Task RespondToRepeatedRequestAsync(
        HttpContext context,
        RegistroIdempotencia existing,
        string path,
        string requestHash)
    {
        if (!string.Equals(existing.HashSolicitud, requestHash, StringComparison.OrdinalIgnoreCase))
        {
            LogKeyReused(logger, path);
            await ApiProblemDetails.WriteAsync(context, IdempotencyErrors.KeyReused);
            return;
        }

        if (existing.Estado == EstadoRegistroIdempotencia.EnProceso || existing.CodigoEstadoRespuesta is null)
        {
            LogKeyInProgress(logger, path);
            await ApiProblemDetails.WriteAsync(context, IdempotencyErrors.InProgress);
            return;
        }

        LogResponseReplayed(logger, path, existing.CodigoEstadoRespuesta.Value);

        context.Response.StatusCode = existing.CodigoEstadoRespuesta.Value;
        foreach (var (name, value) in DeserializeHeaders(existing.EncabezadosRespuesta))
        {
            context.Response.Headers[name] = value;
        }

        context.Response.Headers[ReplayedHeaderName] = "true";

        if (!string.IsNullOrEmpty(existing.CuerpoRespuesta))
        {
            await context.Response.WriteAsync(existing.CuerpoRespuesta, Encoding.UTF8, context.RequestAborted);
        }
    }

    // Caracteres ASCII visibles: evita claves con espacios, saltos de línea o caracteres de control.
    internal static bool IsValidKey(string? key) =>
        !string.IsNullOrEmpty(key) && key.Length <= MaxKeyLength && key.All(character => character is > ' ' and <= '~');

    // La misma clave en otra ruta es independiente; /api/Equipos/ y /api/equipos son la misma ruta.
    internal static string NormalizePath(PathString path) =>
        (path.Value ?? string.Empty).TrimEnd('/').ToLowerInvariant();

    /// <summary>
    /// SHA-256 del cuerpo. Si es JSON se normaliza (sin espacios ni saltos de línea), de modo que el mismo
    /// contenido con distinto formato se considera la misma petición.
    /// </summary>
    internal static async Task<string> ComputeRequestHashAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        // El cuerpo se lee aquí y luego otra vez en el model binding de MVC.
        request.EnableBuffering();

        await using var content = new MemoryStream();
        await request.Body.CopyToAsync(content, cancellationToken);
        request.Body.Position = 0;

        var body = content.ToArray();
        return Convert.ToHexString(SHA256.HashData(NormalizeJson(body) ?? body));
    }

    private static byte[]? NormalizeJson(byte[] body)
    {
        if (body.Length == 0)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var output = new ArrayBufferWriter<byte>(body.Length);
            using (var writer = new Utf8JsonWriter(output))
            {
                document.WriteTo(writer);
            }

            return output.WrittenSpan.ToArray();
        }
        catch (JsonException)
        {
            // No es JSON válido: se usa el cuerpo tal cual (MVC responderá 400 y esa respuesta también se guarda).
            return null;
        }
    }

    private static string SerializeHeaders(IHeaderDictionary headers) =>
        JsonSerializer.Serialize(ReplayedHeaders
            .Where(name => headers.ContainsKey(name))
            .ToDictionary(name => name, name => headers[name].ToString()));

    private static Dictionary<string, string> DeserializeHeaders(string? json) =>
        string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];

    [LoggerMessage(Level = LogLevel.Debug, Message = "Idempotencia: respuesta {StatusCode} guardada para POST {Path}")]
    private static partial void LogResponseStored(ILogger logger, string path, int statusCode);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Idempotencia: POST {Path} repetido; se devuelve la respuesta original {StatusCode}")]
    private static partial void LogResponseReplayed(ILogger logger, string path, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotencia: clave reutilizada con un cuerpo distinto en POST {Path}")]
    private static partial void LogKeyReused(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotencia: POST {Path} con una clave que sigue en proceso")]
    private static partial void LogKeyInProgress(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Idempotencia: no se pudo guardar la respuesta de POST {Path}")]
    private static partial void LogStoreFailed(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotencia: POST {Path} terminó con {StatusCode}; la clave se libera para reintentar")]
    private static partial void LogKeyReleased(ILogger logger, string path, int statusCode);
}
