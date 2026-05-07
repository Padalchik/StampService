using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Screens;

internal sealed class ScreenMessageRenderer : IScreenMessageRenderer
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<ScreenMessageRenderer> _logger;

    public ScreenMessageRenderer(
        ITelegramBotClient bot,
        ILogger<ScreenMessageRenderer> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public async Task<Message> RenderAsync(
        UpdateContext context,
        ScreenView view,
        InlineKeyboardMarkup? keyboard,
        int? existingMessageId,
        ScreenMediaType oldMediaType,
        ScreenMediaType newMediaType)
    {
        if (existingMessageId is null)
        {
            return await SendNewAsync(context, view, keyboard);
        }

        if (oldMediaType == ScreenMediaType.None && newMediaType == ScreenMediaType.None)
        {
            return await EditTextAsync(context, existingMessageId.Value, view, keyboard);
        }

        if (oldMediaType == newMediaType && oldMediaType != ScreenMediaType.None)
        {
            return await EditMediaAsync(context, existingMessageId.Value, view, keyboard);
        }

        await TryDeleteAsync(context, existingMessageId.Value);
        return await SendNewAsync(context, view, keyboard);
    }

    private async Task<Message> SendNewAsync(UpdateContext context, ScreenView view, InlineKeyboardMarkup? keyboard)
    {
        if (view.MediaType == ScreenMediaType.None)
        {
            return await _bot.SendMessage(
                context.ChatId,
                view.Text,
                replyMarkup: keyboard,
                parseMode: view.ParseMode,
                cancellationToken: context.CancellationToken);
        }

        return view.MediaType switch
        {
            ScreenMediaType.Photo => await _bot.SendPhoto(
                context.ChatId,
                view.Media!,
                caption: view.Text,
                replyMarkup: keyboard,
                parseMode: view.ParseMode,
                cancellationToken: context.CancellationToken),

            ScreenMediaType.Video => await _bot.SendVideo(
                context.ChatId,
                view.Media!,
                caption: view.Text,
                replyMarkup: keyboard,
                parseMode: view.ParseMode,
                cancellationToken: context.CancellationToken),

            ScreenMediaType.Animation => await _bot.SendAnimation(
                context.ChatId,
                view.Media!,
                caption: view.Text,
                replyMarkup: keyboard,
                parseMode: view.ParseMode,
                cancellationToken: context.CancellationToken),

            ScreenMediaType.Document => await _bot.SendDocument(
                context.ChatId,
                view.Media!,
                caption: view.Text,
                replyMarkup: keyboard,
                parseMode: view.ParseMode,
                cancellationToken: context.CancellationToken),

            _ => await _bot.SendMessage(
                context.ChatId,
                view.Text,
                replyMarkup: keyboard,
                parseMode: view.ParseMode,
                cancellationToken: context.CancellationToken)
        };
    }

    private async Task<Message> EditTextAsync(
        UpdateContext context, int messageId, ScreenView view, InlineKeyboardMarkup? keyboard)
    {
        try
        {
            return await _bot.EditMessageText(
                context.ChatId,
                messageId,
                view.Text,
                replyMarkup: keyboard,
                parseMode: view.ParseMode,
                cancellationToken: context.CancellationToken);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message is not modified"))
        {
            _logger.LogDebug("Message {MessageId} is not modified, ignoring", messageId);
            Message? existingMessage = context.Update.CallbackQuery?.Message;
            return existingMessage ?? await SendNewAsync(context, view, keyboard);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to edit text message {MessageId}, sending new message", messageId);
            await TryDeleteAsync(context, messageId);
            return await SendNewAsync(context, view, keyboard);
        }
    }

    private async Task<Message> EditMediaAsync(
        UpdateContext context, int messageId, ScreenView view, InlineKeyboardMarkup? keyboard)
    {
        try
        {
            InputMedia inputMedia = view.MediaType switch
            {
                ScreenMediaType.Photo => new InputMediaPhoto(view.Media!)
                {
                    Caption = view.Text,
                    ParseMode = view.ParseMode
                },
                ScreenMediaType.Video => new InputMediaVideo(view.Media!)
                {
                    Caption = view.Text,
                    ParseMode = view.ParseMode
                },
                ScreenMediaType.Animation => new InputMediaAnimation(view.Media!)
                {
                    Caption = view.Text,
                    ParseMode = view.ParseMode
                },
                ScreenMediaType.Document => new InputMediaDocument(view.Media!)
                {
                    Caption = view.Text,
                    ParseMode = view.ParseMode
                },
                _ => throw new InvalidOperationException($"Unsupported media type: {view.MediaType}")
            };

            return await _bot.EditMessageMedia(
                context.ChatId,
                messageId,
                inputMedia,
                replyMarkup: keyboard,
                cancellationToken: context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to edit media message {MessageId}, sending new message", messageId);
            await TryDeleteAsync(context, messageId);
            return await SendNewAsync(context, view, keyboard);
        }
    }

    private async Task TryDeleteAsync(UpdateContext context, int messageId)
    {
        try
        {
            await _bot.DeleteMessage(context.ChatId, messageId, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not delete message {MessageId} in chat {ChatId}", messageId, context.ChatId);
        }
    }
}