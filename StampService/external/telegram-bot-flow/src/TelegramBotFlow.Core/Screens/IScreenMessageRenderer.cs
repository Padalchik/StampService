using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Screens;

internal interface IScreenMessageRenderer
{
    Task<Message> RenderAsync(
        UpdateContext context,
        ScreenView view,
        InlineKeyboardMarkup? keyboard,
        int? existingMessageId,
        ScreenMediaType oldMediaType,
        ScreenMediaType newMediaType);
}