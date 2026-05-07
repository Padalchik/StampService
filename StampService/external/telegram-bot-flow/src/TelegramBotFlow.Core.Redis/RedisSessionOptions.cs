namespace TelegramBotFlow.Core.Sessions.Redis;

public sealed class RedisSessionOptions
{
    public const string SECTION_NAME = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";

    public int? SessionTtlMinutes { get; set; }

    public TimeSpan? SessionTtl =>
        SessionTtlMinutes is > 0 ? TimeSpan.FromMinutes(SessionTtlMinutes.Value) : null;
}