using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MundialitoCorporativo.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra las implementaciones de los contratos de Application: EF Core (escritura), Dapper (lectura),
    /// Unit of Work, almacenamiento de idempotencia, eventos de dominio y seed de datos.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
