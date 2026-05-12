using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Queries.GetUserMetricBalances;
using StampService.Application.Users.Commands.CreateRedemptionCode;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.Contracts.DTOs.Users;
using StampService.TelegramBot.Features.Wallet.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Wallet.Screens;

public sealed class MyWalletScreen : IScreen
{
    public const string ForceRefreshCodeSessionKey = "wallet.force_refresh_code";

    private readonly IQueryHandler<UserMetricBalancesResponse, GetUserMetricBalancesQuery> _balancesHandler;
    private readonly ICommandHandler<CreateRedemptionCodeResponse, CreateRedemptionCodeCommand> _createCodeHandler;
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;

    public MyWalletScreen(
        IQueryHandler<UserMetricBalancesResponse, GetUserMetricBalancesQuery> balancesHandler,
        ICommandHandler<CreateRedemptionCodeResponse, CreateRedemptionCodeCommand> createCodeHandler,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler)
    {
        _balancesHandler = balancesHandler;
        _createCodeHandler = createCodeHandler;
        _ensureUserHandler = ensureUserHandler;
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

        var codeResult = await _createCodeHandler.Handle(
            new CreateRedemptionCodeCommand(userResult.Value.UserId, ForceRefresh: forceRefreshCode),
            ctx.CancellationToken);

        if (codeResult.IsFailed)
            return new ScreenView("Не удалось создать код для списания.").BackButton();

        var balancesResult = await _balancesHandler.Handle(
            new GetUserMetricBalancesQuery(userResult.Value.UserId),
            ctx.CancellationToken);

        if (balancesResult.IsFailed)
            return new ScreenView("Не удалось загрузить балансы.").BackButton();

        var view = new ScreenView(
            "<b>Мой кошелёк</b>\n\n" +
            $"CustomerCode: <code>{Html(userResult.Value.CustomerCode)}</code>\n" +
            $"Код для списания: <code>{Html(codeResult.Value.Code)}</code>\n" +
            $"Действует до: {FormatLocalTime(codeResult.Value.ExpiresAtUtc)}\n\n" +
            "<b>Балансы</b>\n\n" +
            BuildBalancesText(balancesResult.Value));

        view.Row().Button<RefreshMyWalletAction>("🔄 Обновить данные");

        foreach (var brandId in GetBrandIds(balancesResult.Value))
        {
            var brandName = GetBrandName(balancesResult.Value, brandId);
            view.Row().Button<ViewWalletBrandHistoryAction, ViewWalletBrandHistoryPayload>(
                $"📈 История: {brandName}",
                new ViewWalletBrandHistoryPayload(brandId, brandName));
        }

        return view.BackButton();
    }

    private static string BuildBalancesText(UserMetricBalancesResponse response)
    {
        if (response.Balances.Count == 0 && response.CoinWallets.Count == 0)
            return "У вас пока нет балансов.";

        var brandBlocks = new List<string>();
        foreach (var brandId in GetBrandIds(response)
            .OrderBy(id => GetBrandName(response, id), StringComparer.OrdinalIgnoreCase))
        {
            var brandName = GetBrandName(response, brandId);
            var lines = new List<string>
            {
                $"• <b>{Html(brandName)}</b>"
            };

            foreach (var balance in response.Balances
                .Where(balance => balance.BrandId == brandId)
                .OrderBy(balance => balance.MetricName))
            {
                lines.Add($"  - {Html(balance.MetricName)} {balance.Value}/{balance.RedemptionAmount}");
            }

            var coinValue = response.CoinWallets
                .FirstOrDefault(wallet => wallet.BrandId == brandId)
                ?.Value ?? 0;
            lines.Add($"  - Монетки {coinValue}");
            brandBlocks.Add(string.Join("\n", lines));
        }

        return string.Join("\n\n", brandBlocks);
    }

    private static string GetBrandName(UserMetricBalancesResponse response, Guid brandId)
    {
        return response.Balances.FirstOrDefault(balance => balance.BrandId == brandId)?.BrandName
            ?? response.CoinWallets.First(wallet => wallet.BrandId == brandId).BrandName;
    }

    private static Guid[] GetBrandIds(UserMetricBalancesResponse response)
    {
        return response.Balances
            .Select(balance => balance.BrandId)
            .Concat(response.CoinWallets.Select(wallet => wallet.BrandId))
            .Distinct()
            .ToArray();
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static string FormatLocalTime(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        var local = utc.ToLocalTime();
        return $"{local:HH:mm:ss}";
    }
}
