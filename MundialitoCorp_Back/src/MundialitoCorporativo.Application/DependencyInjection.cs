using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Messaging;

namespace MundialitoCorporativo.Application;

public static class DependencyInjection
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IDomainEventHandler<>)
    ];

    /// <summary>
    /// Registra los servicios de la capa Application: despachador CQRS, handlers de Commands, Queries
    /// y eventos de dominio, y validadores.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        var handlerRegistrations = AssemblyReference.Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType && HandlerInterfaces.Contains(contract.GetGenericTypeDefinition()))
                .Select(contract => (Contract: contract, Implementation: type)));

        foreach (var (contract, implementation) in handlerRegistrations)
        {
            services.AddScoped(contract, implementation);
        }

        services.AddValidatorsFromAssembly(AssemblyReference.Assembly, ServiceLifetime.Scoped, includeInternalTypes: true);

        return services;
    }
}
