using System.Net;
using StampService.TelegramBot.Features.CoinProducts.Actions;
using StampService.TelegramBot.Features.Coins.Actions;
using StampService.TelegramBot.Features.CustomerBalances.Actions;
using StampService.TelegramBot.Features.IssueMetric.Screens;
using StampService.TelegramBot.Features.RedeemMetric.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Brands.Screens;

public sealed class ClientWorkScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var canIssue = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.CanIssueSessionKey) ?? false;
        var canRedeem = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.CanRedeemSessionKey) ?? false;
        var canViewBalances = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.CanViewBalancesSessionKey) ?? false;
        var isMetricsEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsMetricsEnabledSessionKey) ?? true;
        var isCoinsEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsCoinsEnabledSessionKey) ?? true;
        var isCoinProductRedemptionEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsCoinProductRedemptionEnabledSessionKey) ?? true;
        var isManualCoinRedemptionEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsManualCoinRedemptionEnabledSessionKey) ?? false;

        var view = new ScreenView(
            $"<b>{Html(brandName)}</b>\n\n" +
            "Работа с клиентами");

        var hasActions = false;

        if (canIssue && isMetricsEnabled)
        {
            view.Row().NavigateButton<IssueMetricSelectScreen>("🟢 Выдать метрику");
            hasActions = true;
        }

        if (canRedeem && isMetricsEnabled)
        {
            view.Row().NavigateButton<RedeemMetricCodeScreen>("🟡 Списать метрику");
            hasActions = true;
        }

        if (canIssue && isCoinsEnabled)
        {
            view.Row().Button<StartIssueCoinsAction>("🟢 Начислить монетки");
            hasActions = true;
        }

        if (canRedeem && isCoinsEnabled && isCoinProductRedemptionEnabled)
        {
            view.Row().Button<StartPurchaseCoinProductAction>("🟡 Списать за товар");
            hasActions = true;
        }

        if (canRedeem && isCoinsEnabled && isManualCoinRedemptionEnabled)
        {
            view.Row().Button<StartRedeemCoinsAction>("🟡 Списать монетки");
            hasActions = true;
        }

        if (canViewBalances)
        {
            view.Row().Button<StartCustomerBalancesAction>("Балансы клиента");
            hasActions = true;
        }

        if (!hasActions)
            view = new ScreenView($"<b>{Html(brandName)}</b>\n\nНет доступных клиентских действий.");

        return ValueTask.FromResult(view.BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
