using System.Net;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.CoinProducts.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Screens;

public sealed class PurchaseCoinProductConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var customerName = ctx.Session?.Data.GetString(CoinProductSessionKeys.PurchaseCustomerName) ?? "клиент";
        var redemptionCode = ctx.Session?.Data.GetString(CoinProductSessionKeys.PurchaseRedemptionCode) ?? "-";
        var productId = ctx.Session?.Data.Get<Guid>(CoinProductSessionKeys.PurchaseProductId) ?? Guid.Empty;
        var productName = ctx.Session?.Data.GetString(CoinProductSessionKeys.PurchaseProductName) ?? "товар";
        var price = ctx.Session?.Data.Get<int>(CoinProductSessionKeys.PurchaseProductPrice) ?? 0;
        var currentBalance = ctx.Session?.Data.Get<int>(CoinProductSessionKeys.PurchaseCurrentBalance) ?? 0;

        return ValueTask.FromResult(new ScreenView(
            "<b>Подтвердите покупку</b>\n\n" +
            $"Бренд: {Html(brandName)}\n" +
            $"Клиент: {Html(customerName)}\n" +
            $"Товар: {Html(productName)}\n" +
            $"Код списания клиента: <code>{Html(redemptionCode)}</code>\n" +
            $"Монетки: {currentBalance}/{price}")
            .Button<PurchaseCoinProductAction, PurchaseCoinProductPayload>(
                "✅ Подтвердить",
                new PurchaseCoinProductPayload(productId, CanPurchase: true))
            .Row()
            .Button<CancelPurchaseCoinProductAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
