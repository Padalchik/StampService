using System.Net;
using StampService.TelegramBot.Features.CoinProducts.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Screens;

public sealed class EditCoinProductConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var name = ctx.Session?.Data.GetString(CoinProductSessionKeys.EditName) ?? "-";
        var price = ctx.Session?.Data.Get<int>(CoinProductSessionKeys.EditPrice) ?? 0;

        return ValueTask.FromResult(new ScreenView(
            "<b>Сохранить товар?</b>\n\n" +
            $"Название: {Html(name)}\n" +
            $"Цена: {price} монеток")
            .Button<ConfirmEditCoinProductAction>("✅ Подтвердить")
            .Row()
            .Button<CancelEditCoinProductAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
