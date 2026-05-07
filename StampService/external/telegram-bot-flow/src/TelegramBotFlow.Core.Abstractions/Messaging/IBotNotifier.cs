using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotFlow.Core.Messaging;

/// <summary>
/// Sends messages to users outside the update pipeline (proactive messaging).
/// Registered as singleton — inject into IHostedService, background jobs, webhooks.
/// </summary>
public interface IBotNotifier
{
    Task<Message> SendTextAsync(long chatId, string text,
        InlineKeyboardMarkup? keyboard = null,
        ParseMode parseMode = ParseMode.Html,
        CancellationToken ct = default);

    Task<Message> SendPhotoAsync(long chatId, InputFile photo,
        string? caption = null,
        InlineKeyboardMarkup? keyboard = null,
        CancellationToken ct = default);

    Task<Message> SendDocumentAsync(long chatId, InputFile document,
        string? caption = null,
        CancellationToken ct = default);

    Task<Message> CopyMessageAsync(long toChatId, long fromChatId, int messageId,
        CancellationToken ct = default);
}
