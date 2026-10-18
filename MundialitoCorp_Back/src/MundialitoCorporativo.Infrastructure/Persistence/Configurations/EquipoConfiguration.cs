using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundialitoCorporativo.Domain.Equipos;

namespace MundialitoCorporativo.Infrastructure.Persistence.Configurations;

internal sealed class EquipoConfiguration : IEntityTypeConfiguration<Equipo>
{
    public void Configure(EntityTypeBuilder<Equipo> builder)
    {
        builder.ToTable("Equipos", table =>
        {
            table.HasCheckConstraint("CK_Equipos_Nombre_Longitud", "LEN(Nombre) >= 3");
            table.HasCheckConstraint("CK_Equipos_Abreviatura_Longitud", "LEN(Abreviatura) BETWEEN 2 AND 5");
            table.HasCheckConstraint("CK_Equipos_Abreviatura_Mayus", "Abreviatura COLLATE Latin1_General_CS_AS = UPPER(Abreviatura)");
            table.HasCheckConstraint("CK_Equipos_Eliminado", "(Eliminado = 0 AND FechaEliminacion IS NULL) OR (Eliminado = 1 AND FechaEliminacion IS NOT NULL)");
        });

        builder.HasKey(equipo => equipo.Id).HasName("PK_Equipos");
        builder.Property(equipo => equipo.Id).ValueGeneratedNever();
        builder.Property(equipo => equipo.Nombre).HasMaxLength(Equipo.NombreMaxLength).IsRequired();
        builder.Property(equipo => equipo.Abreviatura).HasMaxLength(Equipo.AbreviaturaMaxLength).IsRequired();
        builder.Property(equipo => equipo.Departamento).HasMaxLength(Equipo.DepartamentoMaxLength);

        AuditColumns.ConfigureSoftDelete(builder);
        AuditColumns.Configure(builder);

        builder.HasIndex(equipo => equipo.Nombre).IsUnique().HasFilter("[Eliminado] = 0").HasDatabaseName("UX_Equipos_Nombre");
        builder.HasIndex(equipo => equipo.Abreviatura).IsUnique().HasFilter("[Eliminado] = 0").HasDatabaseName("UX_Equipos_Abreviatura");
        builder.HasIndex(equipo => equipo.Departamento)
            .IncludeProperties(equipo => new { equipo.Nombre, equipo.Abreviatura })
            .HasFilter("[Eliminado] = 0")
            .HasDatabaseName("IX_Equipos_Departamento");
    }
}
