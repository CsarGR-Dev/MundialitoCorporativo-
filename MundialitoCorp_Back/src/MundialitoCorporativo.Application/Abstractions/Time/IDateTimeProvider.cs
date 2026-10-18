namespace MundialitoCorporativo.Application.Abstractions.Time;

/// <summary>
/// Fecha y hora actual en UTC. Se abstrae para poder fijarla en las pruebas.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
