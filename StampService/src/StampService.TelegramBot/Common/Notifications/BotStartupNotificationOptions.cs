namespace StampService.TelegramBot.Common.Notifications;

public sealed class BotStartupNotificationOptions
{
    public const string SectionName = "StartupNotifications";

    public bool Enabled { get; init; } = true;

    public string WebInterfaceUrl { get; init; } = string.Empty;

    public string SeqUrl { get; init; } = string.Empty;
}
