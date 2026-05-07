using StampService.Application.Abstractions;
using StampService.Application.Metrics.Commands.IssueMetric;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
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
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler)
    {
        if (!long.TryParse(ctx.MessageText, out var recipientTelegramUserId) || recipientTelegramUserId <= 0)
        {
            return BotResults.ShowView(new ScreenView(
                "Telegram user id должен быть положительным числом. Попробуйте еще раз.")
                .AwaitInput<EnterIssueRecipientAction>()
                .BackButton());
        }

        var recipientResult = await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                recipientTelegramUserId,
                FirstName: null,
                LastName: null,
                Username: null),
            ctx.CancellationToken);

        if (recipientResult.IsFailed)
        {
            return BotResults.ShowView(new ScreenView(
                "Не удалось создать или найти получателя. Попробуйте еще раз.")
                .AwaitInput<EnterIssueRecipientAction>()
                .BackButton());
        }

        ctx.Session?.Data.Set(IssueMetricSessionKeys.RecipientUserId, recipientResult.Value.UserId);
        ctx.Session?.Data.Set(IssueMetricSessionKeys.RecipientTelegramUserId, recipientTelegramUserId);

        return BotResults.NavigateTo<IssueMetricAmountScreen>();
    }

    private static async Task<IEndpointResult> EnterAmountAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<IssueMetricResponse, IssueMetricCommand> issueMetricHandler)
    {
        if (!int.TryParse(ctx.MessageText, out var amount) || amount <= 0)
        {
            return BotResults.ShowView(new ScreenView(
                "Количество должно быть положительным числом. Попробуйте еще раз.")
                .AwaitInput<EnterIssueAmountAction>()
                .BackButton());
        }

        var metricDefinitionId = ctx.Session?.Data.Get<Guid>(IssueMetricSessionKeys.MetricDefinitionId) ?? Guid.Empty;
        var recipientUserId = ctx.Session?.Data.Get<Guid>(IssueMetricSessionKeys.RecipientUserId) ?? Guid.Empty;

        if (metricDefinitionId == Guid.Empty || recipientUserId == Guid.Empty)
            return BotResults.ShowView(new ScreenView("Сценарий выдачи устарел. Начните заново.").BackButton());

        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var issuerResult = await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (issuerResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось определить сотрудника.").BackButton());

        var issueResult = await issueMetricHandler.Handle(
            new IssueMetricCommand(
                metricDefinitionId,
                issuerResult.Value.UserId,
                new IssueMetricRequest(
                    recipientUserId,
                    amount,
                    "Issued from Telegram bot")),
            ctx.CancellationToken);

        if (issueResult.IsFailed)
            return BotResults.ShowView(new ScreenView("Не удалось выдать метрику.").BackButton());

        ClearIssueSession(ctx);

        return BotResults.ShowView(new ScreenView(
            "<b>Метрика выдана</b>\n\n" +
            $"Количество: {issueResult.Value.Amount}\n" +
            $"Текущий баланс: {issueResult.Value.BalanceValue}")
            .BackButton());
    }

    private static void ClearIssueSession(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.MetricDefinitionId);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.MetricName);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.RecipientUserId);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.RecipientTelegramUserId);
    }
}
