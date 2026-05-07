using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.IntegrationTests.Infrastructure;

/// <summary>
/// Тестовый double для <see cref="IScreenMessageRenderer"/>.
/// Возвращает Message с реальным ID при редактировании и Message { Id = 42 } при новой отправке,
/// чтобы <see cref="TelegramBotFlow.Core.Screens.ScreenManager"/> корректно сохранял NavMessageId
/// без реальных вызовов Telegram API.
/// </summary>
internal sealed class FakeScreenMessageRenderer : IScreenMessageRenderer
{
    public const int DEFAULT_NAV_MESSAGE_ID = 42;

    public Task<Message> RenderAsync(
        UpdateContext context,
        ScreenView view,
        InlineKeyboardMarkup? keyboard,
        int? existingMessageId,
        ScreenMediaType oldMediaType,
        ScreenMediaType newMediaType)
    {
        var message = new Message
        {
            Id = existingMessageId ?? DEFAULT_NAV_MESSAGE_ID,
            Date = DateTime.UtcNow
        };
        return Task.FromResult(message);
    }
}