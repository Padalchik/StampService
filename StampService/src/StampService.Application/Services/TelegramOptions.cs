namespace StampService.Application.Services;

public sealed class TelegramOptions
{
    public string BotToken { get; init; } = string.Empty;

    public string BotUsername { get; init; } = string.Empty;

    public int AuthDataMaxAgeMinutes { get; init; } = 1440;
}
