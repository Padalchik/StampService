using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.MainMenu.Screens;

public sealed class MainMenuScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var username = ctx.Update.Message?.From?.Username;
        var greeting = string.IsNullOrWhiteSpace(username)
            ? "Вы авторизованы в StampService."
            : $"@{username}, вы авторизованы в StampService.";

        return ValueTask.FromResult(new ScreenView(
            $"{greeting}\n\n" +
            "Пока доступен базовый экран бота. Следующим шагом добавим просмотр баланса и историю."));
    }
}
