namespace MundialitoCorporativo.Domain.Common;

/// <summary>
/// Validaciones reutilizables de invariantes. Devuelven un error en lugar de lanzar excepciones.
/// </summary>
internal static class Guard
{
    public static Error? Text(string? value, int minLength, int maxLength, Error error)
    {
        var length = value?.Trim().Length ?? 0;

        return length < minLength || length > maxLength ? error : null;
    }

    public static Error? OptionalText(string? value, int maxLength, Error error) =>
        value is not null && value.Trim().Length > maxLength ? error : null;

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Error? FirstError(params Error?[] errors) => errors.FirstOrDefault(error => error is not null);
}
