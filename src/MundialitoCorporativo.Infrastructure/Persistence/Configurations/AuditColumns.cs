using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MundialitoCorporativo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Columnas de auditoría y concurrencia comunes. Se mapean como shadow properties para no ensuciar el dominio;
/// el UnitOfWork asigna FechaCreacion y FechaModificacion.
/// </summary>
internal static class AuditColumns
{
    public const string FechaCreacion = nameof(FechaCreacion);
    public const string FechaModificacion = nameof(FechaModificacion);
    public const string VersionFila = nameof(VersionFila);

    public static void Configure<TEntity>(EntityTypeBuilder<TEntity> builder, bool withModification = true, bool withRowVersion = true)
        where TEntity : class
    {
        builder.Property<DateTime>(FechaCreacion).HasPrecision(3).IsRequired();

        if (withModification)
        {
            builder.Property<DateTime?>(FechaModificacion).HasPrecision(3);
        }

        if (withRowVersion)
        {
            builder.Property<byte[]>(VersionFila).IsRowVersion();
        }
    }

    public static void ConfigureSoftDelete<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : Domain.Common.SoftDeletableAggregateRoot
    {
        builder.Property(entity => entity.Eliminado).IsRequired();
        builder.Property(entity => entity.FechaEliminacion).HasPrecision(3);
        builder.HasQueryFilter(entity => !entity.Eliminado);
    }
}
