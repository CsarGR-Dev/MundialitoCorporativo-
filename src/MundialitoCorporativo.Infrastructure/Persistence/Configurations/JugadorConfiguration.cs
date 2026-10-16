using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Jugadores;
using MundialitoCorporativo.Domain.PosicionesJuego;

namespace MundialitoCorporativo.Infrastructure.Persistence.Configurations;

internal sealed class JugadorConfiguration : IEntityTypeConfiguration<Jugador>
{
    public void Configure(EntityTypeBuilder<Jugador> builder)
    {
        builder.ToTable("Jugadores", table =>
        {
            table.HasCheckConstraint("CK_Jugadores_Nombres_Longitud", "LEN(Nombres) >= 2");
            table.HasCheckConstraint("CK_Jugadores_Apellidos_Longitud", "LEN(Apellidos) >= 2");
            table.HasCheckConstraint("CK_Jugadores_NumeroCamiseta", "NumeroCamiseta BETWEEN 1 AND 99");
            table.HasCheckConstraint("CK_Jugadores_Eliminado", "(Eliminado = 0 AND FechaEliminacion IS NULL) OR (Eliminado = 1 AND FechaEliminacion IS NOT NULL)");
        });

        builder.HasKey(jugador => jugador.Id).HasName("PK_Jugadores");
        builder.Property(jugador => jugador.Id).ValueGeneratedNever();
        builder.Property(jugador => jugador.PosicionJuegoId).HasColumnType("tinyint");
        builder.Property(jugador => jugador.Nombres).HasMaxLength(Jugador.NombreMaxLength).IsRequired();
        builder.Property(jugador => jugador.Apellidos).HasMaxLength(Jugador.NombreMaxLength).IsRequired();
        builder.Property(jugador => jugador.NumeroCamiseta).HasColumnType("tinyint");
        builder.Property(jugador => jugador.Activo).HasDefaultValue(true);

        AuditColumns.ConfigureSoftDelete(builder);
        AuditColumns.Configure(builder);

        builder.HasOne<Equipo>()
            .WithMany()
            .HasForeignKey(jugador => jugador.EquipoId)
            .HasConstraintName("FK_Jugadores_Equipos")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<PosicionJuego>()
            .WithMany()
            .HasForeignKey(jugador => jugador.PosicionJuegoId)
            .HasConstraintName("FK_Jugadores_PosicionesJuego")
            .OnDelete(DeleteBehavior.NoAction);

        // Permite que Goles valide con una FK compuesta que el goleador pertenece al equipo.
        builder.HasAlternateKey(jugador => new { jugador.Id, jugador.EquipoId }).HasName("UQ_Jugadores_Id_EquipoId");

        builder.HasIndex(jugador => new { jugador.EquipoId, jugador.NumeroCamiseta })
            .IsUnique()
            .HasFilter("[Eliminado] = 0")
            .HasDatabaseName("UX_Jugadores_Equipo_NumeroCamiseta");
        builder.HasIndex(jugador => new { jugador.EquipoId, jugador.Apellidos, jugador.Nombres })
            .IncludeProperties(jugador => new { jugador.NumeroCamiseta, jugador.PosicionJuegoId, jugador.Activo })
            .HasFilter("[Eliminado] = 0")
            .HasDatabaseName("IX_Jugadores_Equipo_Apellidos");
        builder.HasIndex(jugador => new { jugador.Apellidos, jugador.Nombres })
            .HasFilter("[Eliminado] = 0")
            .HasDatabaseName("IX_Jugadores_Apellidos");
        builder.HasIndex(jugador => jugador.PosicionJuegoId)
            .HasFilter("[Eliminado] = 0")
            .HasDatabaseName("IX_Jugadores_PosicionJuegoId");
    }
}
