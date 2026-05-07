using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline;

/// <summary>
/// Lightweight builder that collects middleware factories for a conditional branch.
/// Used by <see cref="Hosting.BotApplicationBuilder.UseWhen"/> to configure the branch pipeline.
/// </summary>
public sealed class MiddlewareBranchBuilder
{
    internal readonly List<Func<UpdateDelegate, UpdateDelegate>> Middlewares = [];

    /// <summary>
    /// DI service collection (for resolving DI-based middleware).
    /// </summary>
    public IServiceCollection Services { get; }

    internal MiddlewareBranchBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Adds a middleware factory to the branch pipeline.
    /// </summary>
    /// <param name="middleware">Middleware factory delegate.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public MiddlewareBranchBuilder Use(Func<UpdateDelegate, UpdateDelegate> middleware)
    {
        Middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Adds a DI-resolved middleware to the branch pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">Middleware type implementing <see cref="IUpdateMiddleware"/>.</typeparam>
    /// <returns>This builder for fluent chaining.</returns>
    public MiddlewareBranchBuilder Use<TMiddleware>() where TMiddleware : IUpdateMiddleware
    {
        Middlewares.Add(next => async context =>
        {
            TMiddleware middleware = context.RequestServices.GetRequiredService<TMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }
}
