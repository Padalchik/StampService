using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

internal sealed class PrivateChatOnlyMiddleware : IUpdateMiddleware
{
    public Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        Chat? chat = context.Update.Type switch
        {
            UpdateType.Message => context.Update.Message!.Chat,
            UpdateType.CallbackQuery => context.Update.CallbackQuery!.Message?.Chat,
            UpdateType.EditedMessage => context.Update.EditedMessage!.Chat,
            UpdateType.ChannelPost => context.Update.ChannelPost!.Chat,
            _ => null
        };

        // Разрешаем только приватные чаты
        if (chat is not null && chat.Type != ChatType.Private)
        {
            return Task.CompletedTask; // Дропаем апдейт
        }

        return next(context);
    }
}