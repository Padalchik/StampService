using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Users;
using StampService.Contracts.DTOs.Wallet;
using StampService.Application.Wallet.Commands.OpenUserWallet;
using StampService.TelegramBot.Common.UI;
using StampService.TelegramBot.Features.Wallet.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Wallet.Screens;

public sealed class MyWalletScreen : IScreen
{
    public const string ForceRefreshCodeSessionKey = "wallet.force_refresh_code";

    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly ICommandHandler<UserWalletResponse, OpenUserWalletCommand> _openWalletHandler;

    public MyWalletScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<UserWalletResponse, OpenUserWalletCommand> openWalletHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _openWalletHandler = openWalletHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var userResult = await _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (userResult.IsFailed)
            return new ScreenView("Не удалось определить пользователя.").BackButton();

        var forceRefreshCode = ctx.Session?.Data.Get<bool>(ForceRefreshCodeSessionKey) ?? false;
        ctx.Session?.Data.Remove(ForceRefreshCodeSessionKey);

        var walletResult = await _openWalletHandler.Handle(
            new OpenUserWalletCommand(userResult.Value.UserId, forceRefreshCode),
            ctx.CancellationToken);
        if (walletResult.IsFailed)
            return new ScreenView("Не удалось загрузить кошелёк.").BackButton();

        var wallet = walletResult.Value;
        var view = new ScreenView(
            $"<b>{BotMenuLabels.MyWallet}</b>\n\n" +
            $"Код списания: <code>{Html(wallet.RedemptionCode.Code)}</code>\n" +
            $"Действует до: {FormatLocalTime(wallet.RedemptionCode.ExpiresAtUtc)}\n\n" +
            BuildOverviewText(wallet));

        view.Row().Button<RefreshMyWalletAction>("🔄 Обновить данные");

        foreach (var brand in wallet.Brands)
        {
            view.Row().Button<ViewWalletBrandRewardsAction, ViewWalletBrandRewardsPayload>(
                $"▶️ {brand.BrandName}",
                new ViewWalletBrandRewardsPayload(brand.BrandId, brand.BrandName));
        }

        return view.BackButton();
    }

    private static string BuildOverviewText(UserWalletResponse response)
    {
        if (response.Brands.Count == 0)
            return "<b>Балансы</b>\n\nУ вас пока нет балансов.";

        var brandBlocks = response.Brands.Select(brand =>
        {
            var lines = new List<string> { $"• <b>{Html(brand.BrandName)}</b>" };

            if (brand.IsCoinsEnabled)
                lines.Add($"  - Монетки {brand.CoinBalance}");

            if (brand.IsCoinsEnabled && brand.AvailableCoinProducts.Count > 0)
            {
                lines.Add("  - Доступные товары:");
                lines.AddRange(brand.AvailableCoinProducts.Select(product =>
                    $"    ✅ {Html(product.ProductName)} · {product.Price} монеток"));
            }

            if (brand.IsMetricsEnabled && brand.AvailableMetrics.Count > 0)
            {
                lines.Add("  - Доступные метрики:");
                lines.AddRange(brand.AvailableMetrics.Select(metric =>
                    $"    ✅ {Html(metric.MetricName)} · {metric.CurrentBalance}/{metric.RequiredAmount}"));
            }

            if (brand.AvailableCoinProducts.Count == 0 && brand.AvailableMetrics.Count == 0)
                lines.Add("  - Доступных наград пока нет");

            return string.Join("\n", lines);
        });

        return "<b>Балансы и доступные награды</b>\n\n" +
            string.Join("\n\n", brandBlocks);
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static string FormatLocalTime(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        var local = utc.ToLocalTime();
        return $"{local:HH:mm:ss}";
    }
}
