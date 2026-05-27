using System.Globalization;
using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Coins.Commands.IssueCoins;
using StampService.Application.Coins.Commands.RedeemCoins;
using StampService.Application.Coins.Queries.GetCoinHistory;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Coins;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Notifications;
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

namespace StampService.TelegramBot.Features.Coins.Endpoints;

public sealed class CoinEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<StartIssueCoinsAction>(StartIssueAsync);
        app.MapAction<StartRedeemCoinsAction>(StartRedeemAsync);
        app.MapInput<EnterCoinCustomerPhoneAction>(EnterCustomerPhoneAsync);
        app.MapInput<EnterCoinRedemptionCodeAction>(EnterRedemptionCodeAsync);
        app.MapInput<EnterCoinAmountAction>(EnterAmountAsync);
        app.MapInput<EnterCoinCommentAction>(EnterCommentAsync);
        app.MapAction<ConfirmIssueCoinsAction>(ConfirmIssueAsync);
        app.MapAction<ConfirmRedeemCoinsAction>(ConfirmRedeemAsync);
        app.MapAction<CancelCoinOperationAction>(CancelAsync);
        app.MapAction<ViewCoinHistoryAction, ViewCoinHistoryPayload>(ViewHistoryAsync);
    }

    private static Task<IEndpointResult> StartIssueAsync(UpdateContext ctx)
    {
        ClearOperation(ctx);
        return Task.FromResult(BotResults.NavigateTo<CoinCustomerPhoneScreen>());
    }

    private static Task<IEndpointResult> StartRedeemAsync(UpdateContext ctx)
    {
        ClearOperation(ctx);
        return Task.FromResult(BotResults.NavigateTo<CoinRedemptionCodeScreen>());
    }

    private static async Task<IEndpointResult> EnterCustomerPhoneAsync(UpdateContext ctx)
    {
        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            ctx.MessageText ?? string.Empty,
            "phoneNumber");

        if (phoneNumberResult.IsFailed)
            return await Retry<CoinCustomerPhoneScreen, EnterCoinCustomerPhoneAction>("Введите телефон клиента в международном формате, например +7 999 123-45-67.");

        ctx.Session?.Data.Set(CoinSessionKeys.CustomerPhoneNumber, phoneNumberResult.Value);
        return BotInputResults.DeleteInputThen(BotResults.NavigateTo<CoinAmountScreen>());
    }

    private static async Task<IEndpointResult> EnterRedemptionCodeAsync(UpdateContext ctx)
    {
        var code = ctx.MessageText?.Trim() ?? string.Empty;
        if (!DomainRedemptionCode.IsValidCode(code))
            return await Retry<CoinRedemptionCodeScreen, EnterCoinRedemptionCodeAction>("Код списания должен состоять из 4 цифр.");

        ctx.Session?.Data.Set(CoinSessionKeys.RedemptionCode, code);
        return BotInputResults.DeleteInputThen(BotResults.NavigateTo<CoinRedeemAmountScreen>());
    }

    private static Task<IEndpointResult> EnterAmountAsync(UpdateContext ctx)
    {
        var text = ctx.MessageText?.Trim() ?? string.Empty;
        if (!int.TryParse(text, out var amount) || amount <= 0)
            return Retry<CoinAmountScreen, EnterCoinAmountAction>("Количество монеток должно быть положительным числом.");

        ctx.Session?.Data.Set(CoinSessionKeys.Amount, amount);
        var isRedeem = !string.IsNullOrWhiteSpace(ctx.Session?.Data.GetString(CoinSessionKeys.RedemptionCode));
        return Task.FromResult(BotInputResults.DeleteInputThen(
            isRedeem
                ? BotResults.NavigateTo<CoinCommentScreen>()
                : BotResults.NavigateTo<CoinConfirmScreen>()));
    }

    private static Task<IEndpointResult> EnterCommentAsync(UpdateContext ctx)
    {
        var comment = ctx.MessageText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(comment))
            return Retry<CoinCommentScreen, EnterCoinCommentAction>("Назначение списания обязательно.");

        ctx.Session?.Data.Set(CoinSessionKeys.Comment, comment);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<CoinRedeemConfirmScreen>()));
    }

    private static async Task<IEndpointResult> ConfirmIssueAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<CoinOperationResponse, IssueCoinsByPhoneCommand> issueHandler)
    {
        var brandId = GetBrandId(ctx);
        var customerPhoneNumber = ctx.Session?.Data.GetString(CoinSessionKeys.CustomerPhoneNumber) ?? string.Empty;
        var amount = ctx.Session?.Data.Get<int>(CoinSessionKeys.Amount) ?? 0;
        const string comment = "Issue coins";

        if (brandId == Guid.Empty
            || !PhoneNumberNormalizer.NormalizeForAuth(customerPhoneNumber).IsSuccess
            || amount <= 0)
            return BotResults.ShowView(new ScreenView("Сценарий начисления монеток устарел. Начните заново.").BackButton());

        var actorUserId = await GetActorUserIdAsync(ctx, ensureUserHandler);
        if (actorUserId is null)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var result = await issueHandler.Handle(
            new IssueCoinsByPhoneCommand(
                brandId,
                actorUserId.Value,
                new IssueCoinsByPhoneRequest(customerPhoneNumber, amount, comment)),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось начислить монетки: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        ClearOperation(ctx);
        return BotResults.ShowView(OperationResultView("Монетки начислены", result.Value));
    }

    private static async Task<IEndpointResult> ConfirmRedeemAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<CoinOperationResponse, RedeemCoinsCommand> redeemHandler,
        ICustomerNotificationService customerNotificationService)
    {
        var brandId = GetBrandId(ctx);
        var redemptionCode = ctx.Session?.Data.GetString(CoinSessionKeys.RedemptionCode) ?? string.Empty;
        var amount = ctx.Session?.Data.Get<int>(CoinSessionKeys.Amount) ?? 0;
        var comment = ctx.Session?.Data.GetString(CoinSessionKeys.Comment) ?? string.Empty;

        if (brandId == Guid.Empty || !DomainRedemptionCode.IsValidCode(redemptionCode) || amount <= 0 || string.IsNullOrWhiteSpace(comment))
            return BotResults.ShowView(new ScreenView("Сценарий списания монеток устарел. Начните заново.").BackButton());

        var actorUserId = await GetActorUserIdAsync(ctx, ensureUserHandler);
        if (actorUserId is null)
            return BotResults.ShowView(new ScreenView("Не удалось определить пользователя.").BackButton());

        var result = await redeemHandler.Handle(
            new RedeemCoinsCommand(brandId, actorUserId.Value, redemptionCode, amount, comment),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось списать монетки: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        await customerNotificationService.NotifyCoinsRedeemedAsync(
            result.Value,
            brandName,
            comment,
            ctx.CancellationToken);

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
            new GetCoinHistoryQuery(GetBrandId(ctx), actorUserId.Value, payload.CustomerPhoneNumber, Skip: 0, Take: 10),
            ctx.CancellationToken);

        if (result.IsFailed)
            return BotResults.ShowView(new ScreenView($"Не удалось загрузить историю монеток: {BotErrorFormatter.Format(result.Errors)}").BackButton());

        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var title = $"<b>{Html(brandName)}</b>\nМонетки · <code>{Html(payload.CustomerPhoneNumber)}</code>";
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

            return $"{marker} {date}: {sign}{transaction.Amount} монеток{comment}";
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
            $"Клиент: {Html(response.UserName)}\n" +
            $"Количество: {response.Amount}\n" +
            $"Баланс: {response.BalanceValue}")
            .NavigateButton<ClientWorkScreen>("К работе с клиентами");
    }

    private static Task<IEndpointResult> Retry<TScreen, TAction>(string message)
        where TScreen : IScreen
        where TAction : IBotAction
    {
        return BotEndpointHelpers.RetryInput<TScreen, TAction>(message);
    }

    private static async Task<Guid?> GetActorUserIdAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler)
    {
        var result = await BotEndpointHelpers.EnsureUserAsync(ctx, ensureUserHandler);

        return result.IsSuccess ? result.Value.UserId : null;
    }

    private static Guid GetBrandId(UpdateContext ctx)
    {
        return ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
    }

    private static void ClearOperation(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(CoinSessionKeys.CustomerPhoneNumber);
        ctx.Session?.Data.Remove(CoinSessionKeys.RedemptionCode);
        ctx.Session?.Data.Remove(CoinSessionKeys.Amount);
        ctx.Session?.Data.Remove(CoinSessionKeys.Comment);
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static bool IsAutoComment(string value)
    {
        return value is "Issue coins" or "Purchase product" or "Manual coin redemption";
    }
}
