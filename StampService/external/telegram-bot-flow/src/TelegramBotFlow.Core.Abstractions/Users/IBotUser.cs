namespace TelegramBotFlow.Core.Users;

/// <summary>
/// Contract for a bot user entity tracked in storage.
/// Implement on your user class (or inherit from BotUser in Data.Postgres).
/// </summary>
public interface IBotUser
{
    long TelegramId { get; init; }
    bool IsBlocked { get; set; }
    DateTime JoinedAt { get; }
}
