using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBotFlow.Core.UI;

public sealed class MenuBuilder
{
    private readonly List<BotCommand> _commands = [];
    private readonly List<BotCommand> _adminCommands = [];

    public MenuBuilder Command(string command, string description)
    {
        _commands.Add(new BotCommand
        {
            Command = command.TrimStart('/').ToLowerInvariant(),
            Description = description
        });

        return this;
    }

    public MenuBuilder AdminCommand(string command, string description)
    {
        _adminCommands.Add(new BotCommand
        {
            Command = command.TrimStart('/').ToLowerInvariant(),
            Description = description
        });

        return this;
    }

    public async Task ApplyAsync(ITelegramBotClient bot, long[] adminIds, CancellationToken cancellationToken = default)
    {
        await bot.SetMyCommands(_commands, cancellationToken: cancellationToken);

        if (_adminCommands.Count == 0 || adminIds.Length == 0)
            return;

        List<BotCommand> allAdminCommands = [.. _commands, .. _adminCommands];
        foreach (long adminId in adminIds)
        {
            await bot.SetMyCommands(allAdminCommands,
                scope: new BotCommandScopeChat { ChatId = adminId },
                cancellationToken: cancellationToken);
        }
    }

    public IReadOnlyList<BotCommand> Build() => _commands.AsReadOnly();
}