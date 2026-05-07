using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Context;

internal sealed class BotConfigurationUserAccessPolicy : IUserAccessPolicy
{
    private readonly IOptions<BotConfiguration> _options;

    public BotConfigurationUserAccessPolicy(IOptions<BotConfiguration> options)
    {
        _options = options;
    }

    public bool IsAdmin(UpdateContext context)
    {
        return _options.Value.AdminUserIds.Contains(context.UserId);
    }
}