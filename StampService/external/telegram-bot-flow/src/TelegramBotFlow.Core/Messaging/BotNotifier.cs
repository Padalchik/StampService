using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotFlow.Core.Messaging;

internal sealed class BotNotifier : IBotNotifier
{
    private readonly ITelegramBotClient _bot;

    public BotNotifier(ITelegramBotClient bot)
    {
        _bot = bot;
    }

    public async Task<Message> SendTextAsync(long chatId, string text,
        InlineKeyboardMarkup? keyboard = null,
        ParseMode parseMode = ParseMode.Html,
        CancellationToken ct = default)
    {
        return await _bot.SendMessage(
            chatId,
            text,
            replyMarkup: keyboard,
            parseMode: parseMode,
            cancellationToken: ct);
    }

    public async Task<Message> SendPhotoAsync(long chatId, InputFile photo,
        string? caption = null,
        InlineKeyboardMarkup? keyboard = null,
        CancellationToken ct = default)
    {
        return await _bot.SendPhoto(
            chatId,
            photo,
            caption: caption,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    public async Task<Message> SendDocumentAsync(long chatId, InputFile document,
        string? caption = null,
        CancellationToken ct = default)
    {
        return await _bot.SendDocument(
            chatId,
            document,
            caption: caption,
            cancellationToken: ct);
    }

    public async Task<Message> CopyMessageAsync(long toChatId, long fromChatId, int messageId,
        CancellationToken ct = default)
    {
        MessageId copied = await _bot.CopyMessage(
            toChatId,
            fromChatId,
            messageId,
            cancellationToken: ct);

        return new Message { Id = copied.Id, Chat = new Chat { Id = toChatId } };
    }
}
