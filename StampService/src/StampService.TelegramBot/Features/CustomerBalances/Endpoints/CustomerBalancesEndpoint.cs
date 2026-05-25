using System.Net;
using System.Globalization;
using StampService.Application.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Metrics.Queries.GetBrandCustomerMetricBalances;
using StampService.Application.Metrics.Queries.GetMetricTransactions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Routing;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.Coins.Actions;
using StampService.TelegramBot.Features.CustomerBalances.Actions;
using StampService.TelegramBot.Features.CustomerBalances.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CustomerBalances.Endpoints;

public sealed class CustomerBalancesEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<StartCustomerBalancesAction>(StartAsync);
        app.MapInput<EnterCustomerBalancesPhoneAction>(EnterCustomerPhoneAsync);
        app.MapAction<ViewCustomerBalanceHistoryAction, ViewCustomerBalanceHistoryPayload>(ViewHistoryAsync);
    }

    private static Task<IEndpointResult> StartAsync(UpdateContext ctx)
    {
        return Task.FromResult(BotResults.NavigateTo<CustomerBalancesPhoneScreen>());
    }

    private static async Task<IEndpointResult> EnterCustomerPhoneAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<BrandCustomerMetricBalancesResponse, GetBrandCustomerMetricBalancesQuery> balancesHandler)
    {
        var brandId = ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
        if (brandId == Guid.Empty)
            return BotResults.ShowView(new ScreenView("Бренд не выбран.").BackButton());

        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            ctx.MessageText ?? string.Empty,
            "phoneNumber");

        if (phoneNumberResult.IsFailed)
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                "Введите телефон клиента в международном формате, например +7 999 123-45-67.")
                .AwaitInput<EnterCustomerBalancesPhoneAction>()
                .BackButton()));

        var userResult = await BotEndpointHelpers.EnsureUserAsync(ctx, ensureUserHandler);

        if (userResult.IsFailed)
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                $"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}")
                .AwaitInput<EnterCustomerBalancesPhoneAction>()
                .BackButton()));
        }

        var balancesResult = await balancesHandler.Handle(
            new GetBrandCustomerMetricBalancesQuery(
                userResult.Value.UserId,
                brandId,
                phoneNumberResult.Value),
            ctx.CancellationToken);

        if (balancesResult.IsFailed)
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                $"Не удалось загрузить балансы: {BotErrorFormatter.Format(balancesResult.Errors)}")
                .AwaitInput<EnterCustomerBalancesPhoneAction>()
                .BackButton()));
        }

        return BotInputResults.DeleteInputThen(BotResults.ShowView(BuildBalancesView(ctx, balancesResult.Value)));
    }

    private static async Task<IEndpointResult> ViewHistoryAsync(
        UpdateContext ctx,
        ViewCustomerBalanceHistoryPayload payload,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<MetricTransactionsResponse, GetMetricTransactionsQuery> transactionsHandler)
    {
        var userResult = await BotEndpointHelpers.EnsureUserAsync(ctx, ensureUserHandler);

        if (userResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var transactionsResult = await transactionsHandler.Handle(
            new GetMetricTransactionsQuery(
                payload.MetricDefinitionId,
                payload.CustomerUserId,
                userResult.Value.UserId,
                Skip: 0,
                Take: 10),
            ctx.CancellationToken);

        if (transactionsResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось загрузить историю.").BackButton());

        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var title =
            $"<b>{Html(brandName)}</b>\n" +
            $"{Html(payload.MetricName)}\n" +
            $"Клиент: {Html(payload.CustomerName)} · <code>{Html(payload.CustomerPhoneNumber)}</code>";

        if (transactionsResult.Value.Items.Count == 0)
        {
            return BotResults.ShowView(new ScreenView(
                $"{title}\n\n" +
                "Истории операций пока нет.")
                .NavigateButton<CustomerBalancesPhoneScreen>("Другой клиент")
                .BackButton());
        }

        var lines = transactionsResult.Value.Items.Select(transaction =>
        {
            var isIssue = transaction.TransactionType == "Issue";
            var marker = isIssue ? "🟢" : "🟡";
            var sign = isIssue ? "+" : "-";
            var date = transaction.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            var comment = string.IsNullOrWhiteSpace(transaction.Comment) || IsAutoComment(transaction.Comment)
                ? string.Empty
                : $" - {Html(transaction.Comment)}";

            return $"{marker} {date}: {sign}{transaction.Amount} {Html(payload.MetricName)}{comment}";
        });

        return BotResults.ShowView(new ScreenView(
            $"{title}\n\n" +
            "<b>Последние операции</b>\n" +
            string.Join("\n", lines))
            .NavigateButton<CustomerBalancesPhoneScreen>("Другой клиент")
            .BackButton());
    }

    private static ScreenView BuildBalancesView(
        UpdateContext ctx,
        BrandCustomerMetricBalancesResponse response)
    {
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var isMetricsEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsMetricsEnabledSessionKey) ?? true;
        var isCoinsEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsCoinsEnabledSessionKey) ?? true;
        var activeBalances = response.Balances
            .Where(_ => isMetricsEnabled)
            .Where(balance => balance.IsActive)
            .ToArray();

        var lines = new List<string>();
        if (isMetricsEnabled)
        {
            if (activeBalances.Length == 0)
                lines.Add("В бренде пока нет метрик.");
            else
                lines.AddRange(activeBalances.Select(balance => $"{Html(balance.MetricName)}: {balance.Value}"));
        }

        if (isCoinsEnabled)
            lines.Add($"монетки: {response.CoinBalanceValue}");

        var view = new ScreenView(
            $"<b>{Html(brandName)}</b>\n\n" +
            $"Клиент: {Html(response.CustomerName)} · <code>{Html(response.CustomerPhoneNumber)}</code>\n\n" +
            string.Join("\n", lines));

        foreach (var balance in activeBalances)
        {
            view.Row().Button<ViewCustomerBalanceHistoryAction, ViewCustomerBalanceHistoryPayload>(
                $"📈 История: {balance.MetricName}",
                new ViewCustomerBalanceHistoryPayload(
                    response.CustomerUserId,
                    response.CustomerName,
                    response.CustomerPhoneNumber,
                    balance.MetricDefinitionId,
                    balance.MetricName));
        }

        if (isCoinsEnabled)
        {
            view.Row().Button<ViewCoinHistoryAction, ViewCoinHistoryPayload>(
                "📈 История: монетки",
                new ViewCoinHistoryPayload(response.CustomerPhoneNumber));
        }

        return view.NavigateButton<CustomerBalancesPhoneScreen>("Другой клиент")
            .BackButton();
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static bool IsAutoComment(string value)
    {
        return value is "Issue metric" or "Redeem metric";
    }
}
