using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.Coins.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Coins.Screens;

public sealed class CoinMenuScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var canIssue = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.CanIssueSessionKey) ?? false;
        var canRedeem = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.CanRedeemSessionKey) ?? false;
        var canViewBalances = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.CanViewBalancesSessionKey) ?? false;

        var view = new ScreenView(
            $"<b>{brandName}</b>\n\n" +
            "Монетки");

        if (canIssue)
            view.Button<StartIssueCoinsAction>("Начислить").Row();

        if (canRedeem)
            view.Button<StartRedeemCoinsAction>("Списать").Row();

        if (canViewBalances)
            view.Button<StartCoinBalanceAction>("Баланс клиента").Row();

        return ValueTask.FromResult(view
            .NavigateButton<BrandWorkspaceScreen>("К бренду")
            .BackButton());
    }
}
