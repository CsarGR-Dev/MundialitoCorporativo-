namespace MundialitoCorporativo.Api.Http;

public static class MediaTypes
{
    public const string Json = "application/json";

    /// <summary>JSON Merge Patch (RFC 7396), aceptado en los PATCH.</summary>
    public const string MergePatchJson = "application/merge-patch+json";

    /// <summary>Errores en formato ProblemDetails (RFC 9457).</summary>
    public const string ProblemJson = "application/problem+json";
}
