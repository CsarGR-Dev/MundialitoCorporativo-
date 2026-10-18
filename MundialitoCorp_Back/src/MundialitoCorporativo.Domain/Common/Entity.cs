namespace MundialitoCorporativo.Domain.Common;

/// <summary>
/// Entidad identificada por un Guid generado en el dominio (no en la base de datos).
/// </summary>
public abstract class Entity
{
    protected Entity(Guid id)
    {
        Id = id;
    }

    // Requerido por EF Core para materializar la entidad.
    protected Entity()
    {
    }

    public Guid Id { get; private init; }

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
