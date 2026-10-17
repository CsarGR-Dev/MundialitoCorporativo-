using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MundialitoCorporativo.Application.Abstractions.Data;
using MundialitoCorporativo.Application.Abstractions.Idempotency;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Infrastructure.Data;
using MundialitoCorporativo.Infrastructure.Idempotency;
using MundialitoCorporativo.Infrastructure.Initialization;
using MundialitoCorporativo.Infrastructure.Persistence;
using MundialitoCorporativo.Infrastructure.Persistence.Repositories;
using MundialitoCorporativo.Infrastructure.Queries;
using MundialitoCorporativo.Infrastructure.Seed;
using MundialitoCorporativo.Infrastructure.Time;

namespace MundialitoCorporativo.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    /// <summary>
    /// Registra las implementaciones de los contratos de Application: EF Core (escritura), Dapper (lectura),
    /// Unit of Work, repositorios y reloj del sistema.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Falta la cadena de conexión 'ConnectionStrings:{ConnectionStringName}' en la configuración.");
        }

        // Escritura (Commands): EF Core + Unit of Work.
        services.AddDbContext<MundialitoDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITorneoRepository, TorneoRepository>();
        services.AddScoped<IEquipoRepository, EquipoRepository>();
        services.AddScoped<IJugadorRepository, JugadorRepository>();
        services.AddScoped<IPartidoRepository, PartidoRepository>();
        services.AddScoped<IPosicionJuegoRepository, PosicionJuegoRepository>();

        // Lectura (Queries): Dapper.
        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<ITorneoQueries, TorneoQueries>();
        services.AddScoped<IEquipoQueries, EquipoQueries>();
        services.AddScoped<IJugadorQueries, JugadorQueries>();
        services.AddScoped<IPartidoQueries, PartidoQueries>();
        services.AddScoped<IPosicionJuegoQueries, PosicionJuegoQueries>();

        // Idempotencia de POST: almacenamiento de Idempotency-Key y limpieza periódica.
        services.Configure<IdempotencyOptions>(configuration.GetSection(IdempotencyOptions.SectionName));
        services.AddScoped<IIdempotencyStore, SqlIdempotencyStore>();
        services.AddHostedService<IdempotencyCleanupService>();

        // Preparación de la base al iniciar: esquema de Db/ y seed si está vacía (ítem 9).
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped(serviceProvider => ActivatorUtilities.CreateInstance<DatabaseInitializer>(serviceProvider, connectionString));

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }

    /// <summary>
    /// Crea la base y su esquema si no existen y carga los datos iniciales si está vacía.
    /// Se invoca una vez al iniciar la API, antes de atender peticiones.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken);
    }
}
