namespace MundialitoCorporativo.Domain.Common;

/// <summary>
/// Resultado de una operación sin valor de retorno. Reemplaza el uso de excepciones como control de flujo.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Un resultado exitoso no puede contener un error.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Un resultado fallido debe contener un error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public string? ErrorMessage => IsFailure ? Error.Message : null;

    public string? ErrorCode => IsFailure ? Error.Code : null;

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    public static implicit operator Result(Error error) => Failure(error);
}
