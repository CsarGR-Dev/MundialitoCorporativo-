namespace MundialitoCorporativo.Domain.Common;

/// <summary>
/// Agregado con eliminación lógica. Eliminar dos veces deja el mismo estado (DELETE idempotente).
/// Las propiedades se llaman igual que las columnas Eliminado y FechaEliminacion.
/// </summary>
public abstract class SoftDeletableAggregateRoot : AggregateRoot
{
    protected SoftDeletableAggregateRoot(Guid id)
        : base(id)
    {
    }

    protected SoftDeletableAggregateRoot()
    {
    }

    public bool Eliminado { get; private set; }

    public DateTime? FechaEliminacion { get; private set; }

    protected void MarkAsDeleted(DateTime utcNow)
    {
        if (Eliminado)
        {
            return;
        }

        Eliminado = true;
        FechaEliminacion = utcNow;
    }
}
