namespace MundialitoCorporativo.Domain.Common;

/// <summary>
/// Error de negocio devuelto dentro de un <see cref="Result"/>: mensaje obligatorio y código opcional.
/// </summary>
public record Error
{
    public static readonly Error None = new(ErrorType.None, string.Empty);

    public Error(ErrorType type, string message, string? code = null)
    {
        Type = type;
        Message = message;
        Code = code;
    }

    public ErrorType Type { get; }

    public string Message { get; }

    public string? Code { get; }

    public static Error Validation(string message, string? code = null) => new(ErrorType.Validation, message, code);

    public static Error NotFound(string message, string? code = null) => new(ErrorType.NotFound, message, code);

    public static Error Conflict(string message, string? code = null) => new(ErrorType.Conflict, message, code);

    public static Error Failure(string message, string? code = null) => new(ErrorType.Failure, message, code);
}
