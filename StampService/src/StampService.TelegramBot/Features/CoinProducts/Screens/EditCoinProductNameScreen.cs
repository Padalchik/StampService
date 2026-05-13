using System.Net;
using StampService.TelegramBot.Features.CoinProducts.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Screens;

public sealed class EditCoinProductNameScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.GetString(CoinProductSessionKeys.SelectedProductName) ?? "-";
        return ValueTask.FromResult(new ScreenView(
            "<b>Редактирование товара</b>\n\n" +
            $"Текущее название: {Html(current)}\n" +
            "Введите новое название:")
            .AwaitInput<EnterEditCoinProductNameAction>()
            .Button<KeepEditCoinProductNameAction>("Оставить эти данные")
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
