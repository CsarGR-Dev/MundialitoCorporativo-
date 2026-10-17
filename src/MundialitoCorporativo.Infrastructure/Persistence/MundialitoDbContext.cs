using Microsoft.EntityFrameworkCore;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Jugadores;
using MundialitoCorporativo.Domain.Partidos;
using MundialitoCorporativo.Domain.PosicionesJuego;
using MundialitoCorporativo.Domain.Torneos;
using MundialitoCorporativo.Infrastructure.DomainEvents;
using MundialitoCorporativo.Infrastructure.Persistence.Converters;

namespace MundialitoCorporativo.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core del lado de escritura (Commands). Solo lo usan los repositorios y el UnitOfWork;
/// las Queries leen con Dapper.
/// </summary>
public sealed class MundialitoDbContext(DbContextOptions<MundialitoDbContext> options) : DbContext(options)
{
    public DbSet<Torneo> Torneos => Set<Torneo>();

    public DbSet<Equipo> Equipos => Set<Equipo>();

    public DbSet<Jugador> Jugadores => Set<Jugador>();

    public DbSet<Partido> Partidos => Set<Partido>();

    public DbSet<PosicionJuego> PosicionesJuego => Set<PosicionJuego>();

    public DbSet<RegistroEventoDominio> EventosDominio => Set<RegistroEventoDominio>();

    private const string UseUnitOfWorkMessage = "Use IUnitOfWork.CommitAsync para confirmar los cambios.";

    /// <summary>
    /// Bloquea el guardado directo desde el DbContext, síncrono o asíncrono: los cambios solo se confirman
    /// con UnitOfWork.CommitAsync (requerimiento ítem 4).
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException(UseUnitOfWorkMessage);

    /// <inheritdoc cref="SaveChanges(bool)"/>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(UseUnitOfWorkMessage);

    /// <summary>Único punto de guardado; solo lo invoca el Unit of Work.</summary>
    internal Task<int> SaveChangesFromUnitOfWorkAsync(CancellationToken cancellationToken) =>
        base.SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Modern_Spanish_CI_AS");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MundialitoDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Todas las fechas se guardan en UTC; al leerlas se marcan como UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }
}
