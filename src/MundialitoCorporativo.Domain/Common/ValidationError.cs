namespace MundialitoCorporativo.Domain.Common;

/// <summary>
/// Error de validación que agrupa varios errores de campo (se devuelve como 400 con el detalle por propiedad).
/// </summary>
public sealed record ValidationError : Error
{
    public const string DefaultCode = "Validation.Failed";

    public ValidationError(IReadOnlyCollection<FieldError> errors)
        : base(ErrorType.Validation, "Uno o más campos no son válidos.", DefaultCode)
    {
        Errors = errors;
    }

    public IReadOnlyCollection<FieldError> Errors { get; }
}

public sealed record FieldError(string Field, string Message);
