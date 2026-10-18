using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Infrastructure.Persistence.Configurations;

internal sealed class TorneoConfiguration : IEntityTypeConfiguration<Torneo>
{
    public void Configure(EntityTypeBuilder<Torneo> builder)
    {
        builder.ToTable("Torneos", table =>
        {
            table.HasCheckConstraint("CK_Torneos_Nombre_Longitud", "LEN(Nombre) >= 3");
            table.HasCheckConstraint("CK_Torneos_Fechas", "FechaFin IS NULL OR FechaFin >= FechaInicio");
            table.HasCheckConstraint("CK_Torneos_Estado", "Estado IN ('Planificado', 'EnCurso', 'Finalizado')");
            table.HasCheckConstraint("CK_Torneos_Eliminado", "(Eliminado = 0 AND FechaEliminacion IS NULL) OR (Eliminado = 1 AND FechaEliminacion IS NOT NULL)");
        });

        builder.HasKey(torneo => torneo.Id).HasName("PK_Torneos");
        builder.Property(torneo => torneo.Id).ValueGeneratedNever();
        builder.Property(torneo => torneo.Nombre).HasMaxLength(Torneo.NombreMaxLength).IsRequired();
        builder.Property(torneo => torneo.Descripcion).HasMaxLength(Torneo.DescripcionMaxLength);
        builder.Property(torneo => torneo.Estado).HasConversion<string>().HasMaxLength(20).IsUnicode(false).IsRequired();

        AuditColumns.ConfigureSoftDelete(builder);
        AuditColumns.Configure(builder);

        builder.HasMany(torneo => torneo.Equipos)
            .WithOne()
            .HasForeignKey(inscripcion => inscripcion.TorneoId)
            .HasConstraintName("FK_TorneoEquipos_Torneos")
            .OnDelete(DeleteBehavior.NoAction);

        builder.Navigation(torneo => torneo.Equipos).HasField("_equipos").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(torneo => torneo.Nombre).IsUnique().HasFilter("[Eliminado] = 0").HasDatabaseName("UX_Torneos_Nombre");
        builder.HasIndex(torneo => new { torneo.Estado, torneo.FechaInicio })
            .IncludeProperties(torneo => torneo.Nombre)
            .HasFilter("[Eliminado] = 0")
            .HasDatabaseName("IX_Torneos_Estado");
    }
}

internal sealed class TorneoEquipoConfiguration : IEntityTypeConfiguration<TorneoEquipo>
{
    public void Configure(EntityTypeBuilder<TorneoEquipo> builder)
    {
        builder.ToTable("TorneoEquipos");

        builder.HasKey(inscripcion => new { inscripcion.TorneoId, inscripcion.EquipoId }).HasName("PK_TorneoEquipos");
        builder.Property(inscripcion => inscripcion.FechaInscripcion).HasPrecision(3);

        builder.HasOne<Equipo>()
            .WithMany()
            .HasForeignKey(inscripcion => inscripcion.EquipoId)
            .HasConstraintName("FK_TorneoEquipos_Equipos")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(inscripcion => inscripcion.EquipoId).HasDatabaseName("IX_TorneoEquipos_EquipoId");
    }
}
