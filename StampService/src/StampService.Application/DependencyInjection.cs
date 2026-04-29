using Microsoft.Extensions.DependencyInjection;
using StampService.Application.Abstractions;
using StampService.Application.Services;

namespace StampService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // В DirectoryService здесь используется другая цепочка регистрации;
        // оставляем обработчики раздельно, чтобы каждый selector явно регистрировался по интерфейсам.
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped<ITelegramValidationService, TelegramValidationService>();

        return services;
    }
}
