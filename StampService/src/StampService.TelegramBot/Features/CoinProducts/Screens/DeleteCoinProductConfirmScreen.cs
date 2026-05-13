using System.Net;
using StampService.TelegramBot.Features.CoinProducts.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Screens;

public sealed class DeleteCoinProductConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var name = ctx.Session?.Data.GetString(CoinProductSessionKeys.SelectedProductName) ?? "-";
        return ValueTask.FromResult(new ScreenView(
            "<b>Удалить товар?</b>\n\n" +
            $"{Html(name)}")
            .Button<ConfirmDeleteCoinProductAction>("✅ Подтвердить")
            .Row()
            .Button<CancelDeleteCoinProductAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
