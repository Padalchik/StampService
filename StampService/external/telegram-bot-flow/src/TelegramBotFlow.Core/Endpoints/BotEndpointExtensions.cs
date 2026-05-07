using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Endpoints;

public static class BotEndpointExtensions
{
    public static IServiceCollection AddBotEndpoints(this IServiceCollection services, Assembly assembly)
    {
        IEnumerable<ServiceDescriptor> serviceDescriptors = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                           && type.IsAssignableTo(typeof(IBotEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IBotEndpoint), type));

        services.TryAddEnumerable(serviceDescriptors);

        return services;
    }

    public static BotApplication MapBotEndpoints(this BotApplication app)
    {
        IEnumerable<IBotEndpoint> endpoints = app.Services.GetRequiredService<IEnumerable<IBotEndpoint>>();

        foreach (IBotEndpoint endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}