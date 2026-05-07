using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotFlow.Core.Messaging;

/// <summary>
/// Sends messages to multiple users with concurrency control and error tracking.
/// Registered as singleton — inject into IHostedService, background jobs, webhooks.
/// </summary>
public interface IBotBroadcaster
{
    Task<BroadcastResult> BroadcastAsync(
        IReadOnlyList<long> chatIds,
        Func<long, BotMessage> messageFactory,
        BroadcastOptions? options = null,
        CancellationToken ct = default);
}

/// <summary>
/// Message payload for broadcast operations.
/// </summary>
public record BotMessage(
    string Text,
    InlineKeyboardMarkup? Keyboard = null,
    InputFile? Photo = null,
    ParseMode ParseMode = ParseMode.Html);

/// <summary>
/// Options for controlling broadcast behavior.
/// </summary>
public class BroadcastOptions
{
    /// <summary>Maximum number of concurrent sends.</summary>
    public int MaxConcurrency { get; set; } = 25;

    /// <summary>Called when sending to a specific chat fails.</summary>
    public Action<long, Exception>? OnError { get; set; }

    /// <summary>When true, 403 errors are tracked as blocked users in the result.</summary>
    public bool MarkBlockedUsers { get; set; } = true;
}

/// <summary>
/// Result of a broadcast operation with delivery statistics.
/// </summary>
public record BroadcastResult(
    int TotalSent,
    int TotalFailed,
    IReadOnlyList<long> BlockedUserIds,
    IReadOnlyList<long> FailedChatIds);
