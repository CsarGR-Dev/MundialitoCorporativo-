namespace MundialitoCorporativo.Domain.Common;

/// <summary>
/// Hecho relevante del negocio que ya ocurrió. Se registra y se loguea al confirmar la transacción.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTime OccurredOnUtc { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
