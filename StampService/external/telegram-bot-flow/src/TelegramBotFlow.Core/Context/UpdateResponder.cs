using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Screens;
using ReplyMarkup = Telegram.Bot.Types.ReplyMarkups.ReplyMarkup;

namespace TelegramBotFlow.Core.Context;

internal sealed class UpdateResponder : IUpdateResponder
{
    private readonly ITelegramBotClient _bot;

    public UpdateResponder(ITelegramBotClient bot)
    {
        _bot = bot;
    }

    public async Task<Message> ReplyAsync(
        UpdateContext context,
        string text,
        ReplyMarkup? replyMarkup = null,
        ParseMode parseMode = ParseMode.Html)
    {
        return await _bot.SendMessage(
            context.ChatId,
            text,
            replyMarkup: replyMarkup,
            parseMode: parseMode,
            cancellationToken: context.CancellationToken);
    }

    public async Task EditMessageAsync(
        UpdateContext context,
        string text,
        InlineKeyboardMarkup? replyMarkup = null,
        ParseMode parseMode = ParseMode.Html)
    {
        if (context.MessageId is null)
            return;

        await _bot.EditMessageText(
            context.ChatId,
            context.MessageId.Value,
            text,
            replyMarkup: replyMarkup,
            parseMode: parseMode,
            cancellationToken: context.CancellationToken);
    }

    public async Task EditMessageAsync(
        UpdateContext context,
        int messageId,
        string text,
        InlineKeyboardMarkup? replyMarkup = null,
        ParseMode parseMode = ParseMode.Html)
    {
        await _bot.EditMessageText(
            context.ChatId,
            messageId,
            text,
            replyMarkup: replyMarkup,
            parseMode: parseMode,
            cancellationToken: context.CancellationToken);
    }

    public async Task DeleteMessageAsync(UpdateContext context, int messageId)
    {
        await _bot.DeleteMessage(context.ChatId, messageId, context.CancellationToken);
    }

    public async Task DeleteMessageAsync(UpdateContext context)
    {
        if (context.MessageId is null)
            return;

        await _bot.DeleteMessage(context.ChatId, context.MessageId.Value, context.CancellationToken);
    }

    public async Task AnswerCallbackAsync(UpdateContext context, string? text = null, bool showAlert = false)
    {
        if (context.Update.CallbackQuery is null)
            return;

        await _bot.AnswerCallbackQuery(
            context.Update.CallbackQuery.Id,
            text: text,
            showAlert: showAlert,
            cancellationToken: context.CancellationToken);
    }

    public async Task CopyMessageAsync(UpdateContext context, long fromChatId, int messageId)
    {
        await _bot.CopyMessage(
            context.ChatId,
            fromChatId,
            messageId,
            cancellationToken: context.CancellationToken);
    }

    public async Task ReplaceAnchorWithCopyAsync(
        UpdateContext context,
        long fromChatId,
        int messageId,
        InlineKeyboardMarkup? replyMarkup = null)
    {
        if (context.Session?.Navigation.NavMessageId is { } oldNavId)
            try
            { await _bot.DeleteMessage(context.ChatId, oldNavId, context.CancellationToken); }
            catch (ApiRequestException ex) when (ex.ErrorCode is 400 or 403 or 429)
            {
                // 400: message already deleted
                // 403: bot lacks delete permissions
                // 429: rate limited — skip deletion, not critical
            }

        MessageId copied = await _bot.CopyMessage(
            context.ChatId,
            fromChatId,
            messageId,
            replyMarkup: replyMarkup,
            cancellationToken: context.CancellationToken);

        if (context.Session is not null)
        {
            context.Session.Navigation.NavMessageId = copied.Id;
            context.Session.Navigation.CurrentMediaType = ScreenMediaType.None;
        }
    }
}