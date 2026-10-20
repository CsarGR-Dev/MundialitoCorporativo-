using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Behaviors;
using MundialitoCorporativo.Application.Messaging;

namespace MundialitoCorporativo.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios de la capa Application: MediatR con los handlers de Commands, Queries y
    /// eventos de dominio del ensamblado, el pipeline (observabilidad y validación) y los validadores.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            configuration.Lifetime = ServiceLifetime.Scoped;

            // Orden del pipeline: el primero envuelve al segundo. El span y el log cubren también la validación.
            configuration.AddOpenBehavior(typeof(ObservabilityBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddValidatorsFromAssembly(AssemblyReference.Assembly, ServiceLifetime.Scoped, includeInternalTypes: true);

        return services;
    }
}
