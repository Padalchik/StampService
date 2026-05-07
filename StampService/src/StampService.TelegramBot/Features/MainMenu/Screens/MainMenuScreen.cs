using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.MetricBalances.Screens;

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
            "Выберите действие:")
            .NavigateButton<MyBalancesScreen>("Мои балансы")
            .Row()
            .NavigateButton<MyBrandsScreen>("Рабочие бренды"));
    }
}
