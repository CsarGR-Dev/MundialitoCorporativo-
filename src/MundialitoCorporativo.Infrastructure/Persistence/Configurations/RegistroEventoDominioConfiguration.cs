using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MundialitoCorporativo.Infrastructure.DomainEvents;

namespace MundialitoCorporativo.Infrastructure.Persistence.Configurations;

internal sealed class RegistroEventoDominioConfiguration : IEntityTypeConfiguration<RegistroEventoDominio>
{
    public void Configure(EntityTypeBuilder<RegistroEventoDominio> builder)
    {
        builder.ToTable("EventosDominio", table =>
            table.HasCheckConstraint("CK_EventosDominio_DatosEvento", "ISJSON(DatosEvento) = 1"));

        builder.HasKey(registro => registro.Id).HasName("PK_EventosDominio").IsClustered(false);
        builder.Property(registro => registro.Id).ValueGeneratedNever();
        builder.Property(registro => registro.TipoEvento).HasMaxLength(150).IsUnicode(false).IsRequired();
        builder.Property(registro => registro.TipoAgregado).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(registro => registro.DatosEvento).IsRequired();
        builder.Property(registro => registro.FechaOcurrencia).HasPrecision(3);
        builder.Property(registro => registro.TraceId).HasMaxLength(32).IsUnicode(false);
        builder.Property(registro => registro.CorrelationId).HasMaxLength(64).IsUnicode(false);
        builder.Property(registro => registro.FechaProcesado).HasPrecision(3);

        builder.HasIndex(registro => registro.FechaOcurrencia, "CIX_EventosDominio_FechaOcurrencia").IsClustered();
        builder.HasIndex(registro => registro.FechaOcurrencia, "IX_EventosDominio_Pendientes").HasFilter("[FechaProcesado] IS NULL");
        builder.HasIndex(registro => new { registro.TipoAgregado, registro.AgregadoId })
            .IncludeProperties(registro => registro.TipoEvento)
            .HasDatabaseName("IX_EventosDominio_Agregado");
        builder.HasIndex(registro => registro.TraceId).HasFilter("[TraceId] IS NOT NULL").HasDatabaseName("IX_EventosDominio_TraceId");
    }
}
