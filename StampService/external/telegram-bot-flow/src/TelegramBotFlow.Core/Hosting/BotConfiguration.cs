using Telegram.Bot.Types.Enums;

namespace TelegramBotFlow.Core.Hosting;

public sealed class BotConfiguration
{
    public const string SECTION_NAME = "Bot";

    public required string Token { get; set; }
    public BotMode Mode { get; set; } = BotMode.POLLING;
    public string? WebhookUrl { get; set; }
    public string WebhookPath { get; set; } = "/api/bot/webhook";
    public long[] AdminUserIds { get; set; } = [];
    public long StorageChannelId { get; set; }
    public UpdateType[] AllowedUpdates { get; set; } = [UpdateType.Message, UpdateType.CallbackQuery, UpdateType.MyChatMember];
    public string? WebhookSecretToken { get; set; }

    public int PayloadCacheSize { get; set; } = 500;
    public int SessionLockTimeoutSeconds { get; set; } = 10;
    public int MaxConcurrentUpdates { get; set; } = 100;
    public int MaxNavigationDepth { get; set; } = 20;
    public int UpdateChannelCapacity { get; set; } = 1000;
    public int WizardDefaultTtlMinutes { get; set; } = 60;
    public int ShutdownTimeoutSeconds { get; set; } = 30;
    public int TelegramRateLimitPerSecond { get; set; } = 25;
    public int MaxRetryOnRateLimit { get; set; } = 3;
    public string HealthCheckPath { get; set; } = "/health";

    /// <summary>
    /// Per-attempt timeout for Telegram Bot API HTTP calls. Caps the worst-case
    /// blocking time of a single SendMessage / etc. before Polly considers the
    /// attempt failed and either retries or surfaces a TimeoutRejectedException.
    /// Default 8 seconds — typical SendMessage round-trip is sub-second; spikes
    /// up to a few seconds happen but anything above 8s usually means TG is
    /// degraded and we'd rather fail fast and let the caller decide (typically
    /// Wolverine ScheduleRetry with backoff) than block the handler thread.
    ///
    /// 0 disables the timeout (legacy behavior — pre-2026-05-04 some TBF
    /// consumers observed Polly attempts taking 50s+ on slow TG responses).
    /// </summary>
    public int TelegramRequestTimeoutSeconds { get; set; } = 8;
}

public enum BotMode
{
    POLLING,
    WEBHOOK
}