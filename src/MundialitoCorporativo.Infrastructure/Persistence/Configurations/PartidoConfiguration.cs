using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Jugadores;
using MundialitoCorporativo.Domain.Partidos;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Infrastructure.Persistence.Configurations;

internal sealed class PartidoConfiguration : IEntityTypeConfiguration<Partido>
{
    private const string ActivosFilter = "[Eliminado] = 0 AND [Estado] IN ('Programado', 'Jugado')";

    public void Configure(EntityTypeBuilder<Partido> builder)
    {
        builder.ToTable("Partidos", table =>
        {
            table.HasCheckConstraint("CK_Partidos_EquiposDistintos", "EquipoLocalId <> EquipoVisitanteId");
            table.HasCheckConstraint("CK_Partidos_Estado", "Estado IN ('Programado', 'Jugado', 'Cancelado')");
            table.HasCheckConstraint(
                "CK_Partidos_Resultado",
                "(Estado = 'Jugado' AND GolesLocal IS NOT NULL AND GolesVisitante IS NOT NULL AND FechaJugado IS NOT NULL) " +
                "OR (Estado <> 'Jugado' AND GolesLocal IS NULL AND GolesVisitante IS NULL AND FechaJugado IS NULL)");
            table.HasCheckConstraint(
                "CK_Partidos_Eliminado",
                "(Eliminado = 0 AND FechaEliminacion IS NULL) OR (Eliminado = 1 AND FechaEliminacion IS NOT NULL AND Estado <> 'Jugado')");
        });

        builder.HasKey(partido => partido.Id).HasName("PK_Partidos");
        builder.Property(partido => partido.Id).ValueGeneratedNever();
        builder.Property(partido => partido.FechaProgramada).HasColumnType("datetime2(0)");
        builder.Property(partido => partido.Sede).HasMaxLength(Partido.SedeMaxLength);
        builder.Property(partido => partido.Estado).HasConversion<string>().HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(partido => partido.GolesLocal).HasColumnType("tinyint").HasConversion<byte?>();
        builder.Property(partido => partido.GolesVisitante).HasColumnType("tinyint").HasConversion<byte?>();
        builder.Property(partido => partido.FechaJugado).HasPrecision(3);

        AuditColumns.ConfigureSoftDelete(builder);
        AuditColumns.Configure(builder);

        // FK compuestas: ambos equipos deben estar inscritos en el torneo del partido (RN-18).
        builder.HasOne<TorneoEquipo>()
            .WithMany()
            .HasForeignKey(partido => new { partido.TorneoId, partido.EquipoLocalId })
            .HasPrincipalKey(inscripcion => new { inscripcion.TorneoId, inscripcion.EquipoId })
            .HasConstraintName("FK_Partidos_TorneoEquipoLocal")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<TorneoEquipo>()
            .WithMany()
            .HasForeignKey(partido => new { partido.TorneoId, partido.EquipoVisitanteId })
            .HasPrincipalKey(inscripcion => new { inscripcion.TorneoId, inscripcion.EquipoId })
            .HasConstraintName("FK_Partidos_TorneoEquipoVisitante")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(partido => partido.Goles)
            .WithOne()
            .HasForeignKey(gol => gol.PartidoId)
            .HasConstraintName("FK_Goles_Partidos")
            .OnDelete(DeleteBehavior.NoAction);

        builder.Navigation(partido => partido.Goles).HasField("_goles").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(partido => new { partido.EquipoLocalId, partido.FechaProgramada })
            .IsUnique().HasFilter(ActivosFilter).HasDatabaseName("UX_Partidos_EquipoLocal_FechaProgramada");
        builder.HasIndex(partido => new { partido.EquipoVisitanteId, partido.FechaProgramada })
            .IsUnique().HasFilter(ActivosFilter).HasDatabaseName("UX_Partidos_EquipoVisitante_FechaProgramada");
        builder.HasIndex(partido => new { partido.TorneoId, partido.Estado })
            .IncludeProperties(partido => new { partido.EquipoLocalId, partido.EquipoVisitanteId, partido.GolesLocal, partido.GolesVisitante })
            .HasFilter("[Eliminado] = 0").HasDatabaseName("IX_Partidos_Torneo_Estado");
        builder.HasIndex(partido => new { partido.TorneoId, partido.FechaProgramada })
            .IncludeProperties(partido => new { partido.EquipoLocalId, partido.EquipoVisitanteId, partido.Estado, partido.GolesLocal, partido.GolesVisitante, partido.Sede })
            .HasFilter("[Eliminado] = 0").HasDatabaseName("IX_Partidos_Torneo_FechaProgramada");
        builder.HasIndex(partido => partido.FechaProgramada)
            .IncludeProperties(partido => new { partido.TorneoId, partido.EquipoLocalId, partido.EquipoVisitanteId, partido.Estado, partido.GolesLocal, partido.GolesVisitante, partido.Sede })
            .HasFilter("[Eliminado] = 0").HasDatabaseName("IX_Partidos_FechaProgramada");
        builder.HasIndex(partido => new { partido.Estado, partido.FechaProgramada })
            .IncludeProperties(partido => new { partido.TorneoId, partido.EquipoLocalId, partido.EquipoVisitanteId, partido.GolesLocal, partido.GolesVisitante })
            .HasFilter("[Eliminado] = 0").HasDatabaseName("IX_Partidos_Estado");
    }
}

internal sealed class GolConfiguration : IEntityTypeConfiguration<Gol>
{
    public void Configure(EntityTypeBuilder<Gol> builder)
    {
        builder.ToTable("Goles", table =>
            table.HasCheckConstraint("CK_Goles_Minuto", "Minuto BETWEEN 1 AND 130"));

        builder.HasKey(gol => gol.Id).HasName("PK_Goles");
        builder.Property(gol => gol.Id).ValueGeneratedNever();
        builder.Property(gol => gol.Minuto).HasColumnType("tinyint");

        AuditColumns.Configure(builder, withModification: false, withRowVersion: false);

        builder.HasOne<Equipo>()
            .WithMany()
            .HasForeignKey(gol => gol.EquipoId)
            .HasConstraintName("FK_Goles_Equipos")
            .OnDelete(DeleteBehavior.NoAction);

        // FK compuesta: el jugador debe pertenecer al equipo al que se acredita el gol (RN-08).
        builder.HasOne<Jugador>()
            .WithMany()
            .HasForeignKey(gol => new { gol.JugadorId, gol.EquipoId })
            .HasPrincipalKey(jugador => new { jugador.Id, jugador.EquipoId })
            .HasConstraintName("FK_Goles_Jugadores_Equipo")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(gol => gol.JugadorId).IncludeProperties(gol => new { gol.EquipoId, gol.PartidoId }).HasDatabaseName("IX_Goles_JugadorId");
        builder.HasIndex(gol => gol.PartidoId).IncludeProperties(gol => new { gol.JugadorId, gol.EquipoId, gol.Minuto }).HasDatabaseName("IX_Goles_PartidoId");
        builder.HasIndex(gol => gol.EquipoId).HasDatabaseName("IX_Goles_EquipoId");
    }
}
