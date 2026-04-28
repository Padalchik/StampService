using Microsoft.Extensions.DependencyInjection;
using StampService.Application.Abstractions;

namespace StampService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Select(type => new
            {
                Implementation = type,
                Interfaces = type.GetInterfaces()
                    .Where(interfaceType =>
                        interfaceType.IsGenericType &&
                        (interfaceType.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                         interfaceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                    .ToArray()
            })
            .Where(type => type.Interfaces.Length > 0);

        foreach (var handlerType in handlerTypes)
        {
            foreach (var handlerInterface in handlerType.Interfaces)
                services.AddScoped(handlerInterface, handlerType.Implementation);
        }

        return services;
    }
}
