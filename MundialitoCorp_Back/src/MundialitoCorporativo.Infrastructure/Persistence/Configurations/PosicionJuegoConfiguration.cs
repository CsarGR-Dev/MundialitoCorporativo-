using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundialitoCorporativo.Domain.PosicionesJuego;

namespace MundialitoCorporativo.Infrastructure.Persistence.Configurations;

internal sealed class PosicionJuegoConfiguration : IEntityTypeConfiguration<PosicionJuego>
{
    public void Configure(EntityTypeBuilder<PosicionJuego> builder)
    {
        builder.ToTable("PosicionesJuego", table =>
            table.HasCheckConstraint("CK_PosicionesJuego_Abreviatura_Mayus", "Abreviatura COLLATE Latin1_General_CS_AS = UPPER(Abreviatura)"));

        builder.HasKey(posicion => posicion.Id).HasName("PK_PosicionesJuego");
        builder.Property(posicion => posicion.Id).HasColumnType("tinyint").ValueGeneratedNever();
        builder.Property(posicion => posicion.Nombre).HasMaxLength(30).IsRequired();
        builder.Property(posicion => posicion.Abreviatura).HasMaxLength(5).IsRequired();
        builder.Property(posicion => posicion.Orden).HasColumnType("tinyint");
        builder.Property(posicion => posicion.Activo).HasDefaultValue(true);

        builder.HasIndex(posicion => posicion.Nombre).IsUnique().HasDatabaseName("UQ_PosicionesJuego_Nombre");
        builder.HasIndex(posicion => posicion.Abreviatura).IsUnique().HasDatabaseName("UQ_PosicionesJuego_Abreviatura");

        // Datos de referencia: forman parte de la estructura (igual que el MERGE de 001_DDL_CrearBaseDatos.sql).
        builder.HasData(
            new PosicionJuego(1, "Portero", "POR", 1),
            new PosicionJuego(2, "Defensa", "DEF", 2),
            new PosicionJuego(3, "Mediocampista", "MED", 3),
            new PosicionJuego(4, "Delantero", "DEL", 4));
    }
}
