using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

internal sealed class AccessPolicyMiddleware : IUpdateMiddleware
{
    private readonly IUserAccessPolicy _accessPolicy;

    public AccessPolicyMiddleware(IUserAccessPolicy accessPolicy)
    {
        _accessPolicy = accessPolicy;
    }

    public Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        context.IsAdmin = _accessPolicy.IsAdmin(context);
        return next(context);
    }
}