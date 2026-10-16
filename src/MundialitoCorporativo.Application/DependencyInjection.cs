using Microsoft.Extensions.DependencyInjection;

namespace MundialitoCorporativo.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios de la capa Application: despachador CQRS, handlers de Commands y Queries y validadores.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
