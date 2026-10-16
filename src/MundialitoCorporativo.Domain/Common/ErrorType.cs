namespace MundialitoCorporativo.Domain.Common;

/// <summary>
/// Clasificación del error. La API la traduce a código HTTP: Validation → 400, NotFound → 404, Conflict → 409.
/// </summary>
public enum ErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Failure = 4
}
