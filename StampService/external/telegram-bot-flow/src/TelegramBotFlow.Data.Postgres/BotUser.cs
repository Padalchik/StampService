using TelegramBotFlow.Core.Users;

namespace TelegramBotFlow.Core.Data;

/// <summary>
/// Base user entity tracked by the bot.
/// Inherit from this class to add custom properties (e.g. FirstName, Language).
/// </summary>
public class BotUser : IBotUser
{
    public long TelegramId { get; init; }

    public DateTime JoinedAt { get; init; } = DateTime.UtcNow;

    public bool IsBlocked { get; set; }
}