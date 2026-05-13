using System.Net;
using StampService.TelegramBot.Features.CoinProducts.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Screens;

public sealed class CreateCoinProductConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var name = ctx.Session?.Data.GetString(CoinProductSessionKeys.CreateName) ?? "-";
        var price = ctx.Session?.Data.Get<int>(CoinProductSessionKeys.CreatePrice) ?? 0;

        return ValueTask.FromResult(new ScreenView(
            "<b>Создать товар?</b>\n\n" +
            $"Название: {Html(name)}\n" +
            $"Цена: {price} монеток")
            .Button<ConfirmCreateCoinProductAction>("✅ Подтвердить")
            .Row()
            .Button<CancelCreateCoinProductAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
