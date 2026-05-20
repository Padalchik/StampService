using StampService.Application.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Metrics.Commands.IssueMetric;
using StampService.Application.Users;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Notifications;
using StampService.TelegramBot.Common.Routing;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.IssueMetric.Actions;
using StampService.TelegramBot.Features.IssueMetric.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.IssueMetric.Endpoints;

public sealed class IssueMetricEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<SelectIssueMetricAction, SelectIssueMetricPayload>(SelectMetricAsync);
        app.MapInput<EnterIssueRecipientAction>(EnterRecipientAsync);
        app.MapInput<EnterIssueAmountAction>(EnterAmountAsync);
        app.MapAction<ConfirmIssueMetricAction>(ConfirmAsync);
        app.MapAction<CancelIssueMetricAction>(CancelAsync);
    }

    private static Task<IEndpointResult> SelectMetricAsync(
        UpdateContext ctx,
        SelectIssueMetricPayload payload)
    {
        ctx.Session?.Data.Set(IssueMetricSessionKeys.MetricDefinitionId, payload.MetricDefinitionId);
        ctx.Session?.Data.Set(IssueMetricSessionKeys.MetricName, payload.MetricName);

        return Task.FromResult(BotResults.NavigateTo<IssueMetricRecipientScreen>());
    }

    private static async Task<IEndpointResult> EnterRecipientAsync(
        UpdateContext ctx,
        IRecipientResolver recipientResolver)
    {
        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            ctx.MessageText ?? string.Empty,
            "phoneNumber");

        if (phoneNumberResult.IsFailed)
            return await RetryRecipientInputAsync("Введите телефон клиента в международном формате, например +7 999 123-45-67.");

        var recipientResult = await recipientResolver.ResolveByPhoneAsync(
            phoneNumberResult.Value,
            ctx.CancellationToken);

        if (recipientResult.IsFailed)
            return await RetryRecipientInputAsync("Клиент с таким телефоном не найден. Проверьте номер и попробуйте еще раз.");

        ctx.Session?.Data.Set(IssueMetricSessionKeys.RecipientUserId, recipientResult.Value.UserId);
        ctx.Session?.Data.Set(IssueMetricSessionKeys.RecipientPhoneNumber, phoneNumberResult.Value);

        return BotInputResults.DeleteInputThen(BotResults.NavigateTo<IssueMetricAmountScreen>());
    }

    private static Task<IEndpointResult> EnterAmountAsync(UpdateContext ctx)
    {
        if (!int.TryParse(ctx.MessageText, out var amount) || amount <= 0)
        {
            return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                "Количество должно быть положительным числом. Попробуйте еще раз.")
                .AwaitInput<EnterIssueAmountAction>()
                .BackButton())));
        }

        ctx.Session?.Data.Set(IssueMetricSessionKeys.Amount, amount);

        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<IssueMetricConfirmScreen>()));
    }

    private static async Task<IEndpointResult> ConfirmAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<IssueMetricResponse, IssueMetricCommand> issueMetricHandler,
        ICustomerNotificationService customerNotificationService)
    {
        var metricDefinitionId = ctx.Session?.Data.Get<Guid>(IssueMetricSessionKeys.MetricDefinitionId) ?? Guid.Empty;
        var recipientUserId = ctx.Session?.Data.Get<Guid>(IssueMetricSessionKeys.RecipientUserId) ?? Guid.Empty;
        var amount = ctx.Session?.Data.Get<int>(IssueMetricSessionKeys.Amount) ?? 0;
        const string comment = "Issue metric";

        if (metricDefinitionId == Guid.Empty
            || recipientUserId == Guid.Empty
            || amount <= 0)
        {
            return BotResults.ShowView(new ScreenView("Сценарий выдачи устарел. Начните заново.").BackButton());
        }

        var issuerResult = await BotEndpointHelpers.EnsureUserAsync(ctx, ensureUserHandler);

        if (issuerResult.IsFailed)
        {
            return BotResults.ShowView(new ScreenView(
                $"Не удалось определить сотрудника: {BotErrorFormatter.Format(issuerResult.Errors, BotErrorContext.IssueMetric)}")
                .BackButton());
        }

        var issueResult = await issueMetricHandler.Handle(
            new IssueMetricCommand(
                metricDefinitionId,
                issuerResult.Value.UserId,
                new IssueMetricRequest(
                    recipientUserId,
                    amount,
                    comment)),
            ctx.CancellationToken);

        if (issueResult.IsFailed)
        {
            return BotResults.ShowView(new ScreenView(
                $"Не удалось выдать метрику: {BotErrorFormatter.Format(issueResult.Errors, BotErrorContext.IssueMetric)}")
                .BackButton());
        }

        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var metricName = ctx.Session?.Data.GetString(IssueMetricSessionKeys.MetricName) ?? "метрика";
        await customerNotificationService.NotifyMetricIssuedAsync(
            issueResult.Value,
            brandName,
            metricName,
            ctx.CancellationToken);

        ClearIssueSession(ctx);

        return BotResults.ShowView(new ScreenView(
            "<b>Метрика выдана</b>\n\n" +
            $"Количество: {issueResult.Value.Amount}\n" +
            $"Текущий баланс: {issueResult.Value.BalanceValue}")
            .NavigateButton<IssueMetricSelectScreen>("Выдать еще")
            .Row()
            .MenuButton("Главное меню"));
    }

    private static Task<IEndpointResult> CancelAsync(UpdateContext ctx)
    {
        ClearIssueSession(ctx);

        return Task.FromResult(BotResults.ShowView(new ScreenView("Выдача метрики отменена.")
            .MenuButton("Главное меню")));
    }

    private static void ClearIssueSession(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.MetricDefinitionId);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.MetricName);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.RecipientUserId);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.RecipientPhoneNumber);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.Amount);
    }

    private static Task<IEndpointResult> RetryRecipientInputAsync(string message)
    {
        return BotEndpointHelpers.RetryInput<IssueMetricRecipientScreen, EnterIssueRecipientAction>(message);
    }
}
