using System.Globalization;
using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Coins.Commands.IssueCoins;
using StampService.Application.Coins.Commands.RedeemCoins;
using StampService.Application.Coins.Queries.GetCoinHistory;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Coins;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Routing;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.Coins.Actions;
using StampService.TelegramBot.Features.Coins.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using DomainRedemptionCode = StampService.Domain.User.RedemptionCode;
using UserEntity = StampService.Domain.User.User;

namespace StampService.TelegramBot.Features.Coins.Endpoints;

public sealed class CoinEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<StartIssueCoinsAction>(StartIssueAsync);
        app.MapAction<StartRedeemCoinsAction>(StartRedeemAsync);
        app.MapInput<EnterCoinCustomerCodeAction>(EnterCustomerCodeAsync);
        app.MapInput<EnterCoinRedemptionCodeAction>(EnterRedemptionCodeAsync);
        app.MapInput<EnterCoinAmountAction>(EnterAmountAsync);
        app.MapAction<ConfirmIssueCoinsAction>(ConfirmIssueAsync);
        app.MapAction<ConfirmRedeemCoinsAction>(ConfirmRedeemAsync);
        app.MapAction<CancelCoinOperationAction>(CancelAsync);
        app.MapAction<ViewCoinHistoryAction, ViewCoinHistoryPayload>(ViewHistoryAsync);
    }

    private static Task<IEndpointResult> StartIssueAsync(UpdateContext ctx)
    {
        ClearOperation(ctx);
        ctx.Session?.Data.Set(CoinSessionKeys.Mode, CoinSessionKeys.ModeIssue);
        return Task.FromResult(BotResults.NavigateTo<CoinCustomerCodeScreen>());
    }

    private static Task<IEndpointResult> StartRedeemAsync(UpdateContext ctx)
    {
        ClearOperation(ctx);
        ctx.Session?.Data.Set(CoinSessionKeys.Mode, CoinSessionKeys.ModeRedeem);
        return Task.FromResult(BotResults.NavigateTo<CoinRedemptionCodeScreen>());
    }

    private static async Task<IEndpointResult> EnterCustomerCodeAsync(UpdateContext ctx)
    {
        var customerCode = ctx.MessageText?.Trim() ?? string.Empty;
        if (!UserEntity.IsValidCustomerCode(customerCode))
            return await Retry<CoinCustomerCodeScreen, EnterCoinCustomerCodeAction>("CustomerCode должен состоять из 4 цифр.");

        ctx.Session?.Data.Set(CoinSessionKeys.CustomerCode, customerCode);

        return BotInputResults.DeleteInputThen(BotResults.NavigateTo<CoinAmountScreen>());
    }

    private static Task<IEndpointResult> EnterRedemptionCodeAsync(UpdateContext ctx)
    {
        var code = ctx.MessageText?.Trim() ?? string.Empty;
        if (!DomainRedemptionCode.IsValidCode(code))
            return Retry<CoinRedemptionCodeScreen, EnterCoinRedemptionCodeAction>("Код списания должен состоять из 4 цифр.");

        ctx.Session?.Data.Set(CoinSessionKeys.RedemptionCode, code);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<CoinAmountScreen>()));
    }

    private static Task<IEndpointResult> EnterAmountAsync(UpdateContext ctx)
    {
        var text = ctx.MessageText?.Trim() ?? string.Empty;
        if (!int.TryParse(text, out var amount) || amount <= 0)
            return Retry<CoinAmountScreen, EnterCoinAmountAction>("Количество монеток должно быть положительным числом.");

        ctx.Session?.Data.Set(CoinSessionKeys.Amount, amount);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<CoinConfirmScreen>()));
    }

    private static async Task<IEndpointResult> ConfirmIssueAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<CoinOperationResponse, IssueCoinsCommand> issueHandler)
    {
        var brandId = GetBrandId(ctx);
        var customerCode = ctx.Session?.Data.GetString(CoinSessionKeys.CustomerCode) ?? string.Empty;
        var amount = ctx.Session?.Data.Get<int>(CoinSessionKeys.Amount) ?? 0;
        const string comment = "Issue coins";

        if (brandId == Guid.Empty || !UserEntity.IsValidCustomerCode(customerCode) || amount <= 0)
            return BotResults.ShowView(new ScreenView("Сценарий начисления монеток устарел. Начните заново.").BackButton());

        var actorUserId = await GetActorUserIdAsync(ctx, ensureUserHandler);
        if (actorUserId is null)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var result = await issueHandler.Handle(
            new IssueCoinsCommand(brandId, actorUserId.Value, customerCode, amount, comment),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось начислить монетки: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ClearOperation(ctx);
        return BotResults.ShowView(OperationResultView("Монетки начислены", result.Value));
    }

    private static async Task<IEndpointResult> ConfirmRedeemAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<CoinOperationResponse, RedeemCoinsCommand> redeemHandler)
    {
        var brandId = GetBrandId(ctx);
        var redemptionCode = ctx.Session?.Data.GetString(CoinSessionKeys.RedemptionCode) ?? string.Empty;
        var amount = ctx.Session?.Data.Get<int>(CoinSessionKeys.Amount) ?? 0;
        const string comment = "Redeem coins";

        if (brandId == Guid.Empty || !DomainRedemptionCode.IsValidCode(redemptionCode) || amount <= 0)
            return BotResults.ShowView(new ScreenView("Сценарий списания монеток устарел. Начните заново.").BackButton());

        var actorUserId = await GetActorUserIdAsync(ctx, ensureUserHandler);
        if (actorUserId is null)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var result = await redeemHandler.Handle(
            new RedeemCoinsCommand(brandId, actorUserId.Value, redemptionCode, amount, comment),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось списать монетки: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ClearOperation(ctx);
        return BotResults.ShowView(OperationResultView("Монетки списаны", result.Value));
    }

    private static Task<IEndpointResult> CancelAsync(UpdateContext ctx)
    {
        ClearOperation(ctx);
        return Task.FromResult(BotResults.NavigateTo<ClientWorkScreen>());
    }

    private static async Task<IEndpointResult> ViewHistoryAsync(
        UpdateContext ctx,
        ViewCoinHistoryPayload payload,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<CoinTransactionsResponse, GetCoinHistoryQuery> historyHandler)
    {
        var actorUserId = await GetActorUserIdAsync(ctx, ensureUserHandler);
        if (actorUserId is null)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var result = await historyHandler.Handle(
            new GetCoinHistoryQuery(GetBrandId(ctx), actorUserId.Value, payload.CustomerCode, Skip: 0, Take: 10),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось загрузить историю монеток: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var title = $"<b>{Html(brandName)}</b>\nМонетки · <code>{Html(payload.CustomerCode)}</code>";
        if (result.Value.Items.Count == 0)
        {
            return BotResults.ShowView(new ScreenView(
                $"{title}\n\nИстории операций пока нет.")
                .NavigateButton<ClientWorkScreen>("К работе с клиентами")
                .BackButton());
        }

        var lines = result.Value.Items.Select(transaction =>
        {
            var isIssue = transaction.TransactionType == "Issue";
            var marker = isIssue ? "🟢" : "🟡";
            var sign = isIssue ? "+" : "-";
            var date = transaction.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            var comment = string.IsNullOrWhiteSpace(transaction.Comment) || IsAutoComment(transaction.Comment)
                ? string.Empty
                : $" - {Html(transaction.Comment)}";

            return $"{marker} {date}: {sign}{transaction.Amount} монетки{comment}";
        });

        return BotResults.ShowView(new ScreenView(
            $"{title}\n\n<b>Последние операции</b>\n" +
            string.Join("\n", lines))
            .NavigateButton<ClientWorkScreen>("К работе с клиентами")
            .BackButton());
    }

    private static ScreenView OperationResultView(string title, CoinOperationResponse response)
    {
        return new ScreenView(
            $"<b>{title}</b>\n\n" +
            $"Клиент: {Html(response.UserName)} · <code>{Html(response.CustomerCode)}</code>\n" +
            $"Количество: {response.Amount}\n" +
            $"Баланс: {response.BalanceValue}")
            .NavigateButton<ClientWorkScreen>("К работе с клиентами")
            .Row()
            .NavigateButton<BrandWorkspaceScreen>("К бренду");
    }

    private static Task<IEndpointResult> Retry<TScreen, TAction>(string message)
        where TScreen : IScreen
        where TAction : IBotAction
    {
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(message)
            .AwaitInput<TAction>()
            .BackButton())));
    }

    private static async Task<Guid?> GetActorUserIdAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var result = await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        return result.IsSuccess ? result.Value.UserId : null;
    }

    private static Guid GetBrandId(UpdateContext ctx)
    {
        return ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
    }

    private static void ClearOperation(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(CoinSessionKeys.Mode);
        ctx.Session?.Data.Remove(CoinSessionKeys.CustomerCode);
        ctx.Session?.Data.Remove(CoinSessionKeys.RedemptionCode);
        ctx.Session?.Data.Remove(CoinSessionKeys.Amount);
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static bool IsAutoComment(string value)
    {
        return value is "Issue coins" or "Redeem coins";
    }
}
