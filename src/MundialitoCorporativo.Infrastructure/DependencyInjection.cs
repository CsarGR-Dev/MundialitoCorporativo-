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
using MundialitoCorporativo.Infrastructure.Persistence;
using MundialitoCorporativo.Infrastructure.Persistence.Repositories;
using MundialitoCorporativo.Infrastructure.Queries;
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

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
