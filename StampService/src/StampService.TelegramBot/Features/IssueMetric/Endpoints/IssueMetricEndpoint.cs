using System.Net;
using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Commands.IssueMetric;
using StampService.Application.Users;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Loyalty;
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
        app.MapInput<EnterIssueCommentAction>(EnterCommentAsync);
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
        var recipientResult = await recipientResolver.ResolveAsync(
            ctx.MessageText ?? string.Empty,
            ctx.CancellationToken);

        if (recipientResult.IsFailed)
        {
            return BotResults.ShowView(new ScreenView(
                "Клиентский код должен состоять из 4 цифр и принадлежать существующему пользователю. Попробуйте еще раз.")
                .AwaitInput<EnterIssueRecipientAction>()
                .BackButton());
        }

        ctx.Session?.Data.Set(IssueMetricSessionKeys.RecipientUserId, recipientResult.Value.UserId);
        ctx.Session?.Data.Set(IssueMetricSessionKeys.RecipientCustomerCode, recipientResult.Value.PublicIdentifier);

        return BotResults.NavigateTo<IssueMetricAmountScreen>();
    }

    private static Task<IEndpointResult> EnterAmountAsync(
        UpdateContext ctx)
    {
        if (!int.TryParse(ctx.MessageText, out var amount) || amount <= 0)
        {
            return Task.FromResult(BotResults.ShowView(new ScreenView(
                "Количество должно быть положительным числом. Попробуйте еще раз.")
                .AwaitInput<EnterIssueAmountAction>()
                .BackButton()));
        }

        ctx.Session?.Data.Set(IssueMetricSessionKeys.Amount, amount);

        return Task.FromResult(BotResults.NavigateTo<IssueMetricCommentScreen>());
    }

    private static Task<IEndpointResult> EnterCommentAsync(
        UpdateContext ctx)
    {
        var comment = ctx.MessageText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(comment))
        {
            return Task.FromResult(BotResults.ShowView(new ScreenView(
                "Комментарий обязателен. Попробуйте еще раз.")
                .AwaitInput<EnterIssueCommentAction>()
                .BackButton()));
        }

        if (comment.Length > Constants.MAX_TRANSACTION_COMMENT_LENGTH)
        {
            return Task.FromResult(BotResults.ShowView(new ScreenView(
                $"Комментарий не должен быть длиннее {Constants.MAX_TRANSACTION_COMMENT_LENGTH} символов. Попробуйте еще раз.")
                .AwaitInput<EnterIssueCommentAction>()
                .BackButton()));
        }

        ctx.Session?.Data.Set(IssueMetricSessionKeys.Comment, comment);

        return Task.FromResult(BotResults.NavigateTo<IssueMetricConfirmScreen>());
    }

    private static async Task<IEndpointResult> ConfirmAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<IssueMetricResponse, IssueMetricCommand> issueMetricHandler)
    {
        var metricDefinitionId = ctx.Session?.Data.Get<Guid>(IssueMetricSessionKeys.MetricDefinitionId) ?? Guid.Empty;
        var recipientUserId = ctx.Session?.Data.Get<Guid>(IssueMetricSessionKeys.RecipientUserId) ?? Guid.Empty;
        var amount = ctx.Session?.Data.Get<int>(IssueMetricSessionKeys.Amount) ?? 0;
        var comment = ctx.Session?.Data.GetString(IssueMetricSessionKeys.Comment) ?? string.Empty;

        if (metricDefinitionId == Guid.Empty
            || recipientUserId == Guid.Empty
            || amount <= 0
            || string.IsNullOrWhiteSpace(comment))
        {
            return BotResults.ShowView(new ScreenView("Сценарий выдачи устарел. Начните заново.").BackButton());
        }

        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var issuerResult = await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (issuerResult.IsFailed)
        {
            return BotResults.ShowView(new ScreenView(
                $"Не удалось определить сотрудника: {FormatErrors(issuerResult.Errors)}")
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
                $"Не удалось выдать метрику: {FormatErrors(issueResult.Errors)}")
                .BackButton());
        }

        ClearIssueSession(ctx);

        return BotResults.ShowView(new ScreenView(
            "<b>Метрика выдана</b>\n\n" +
            $"Количество: {issueResult.Value.Amount}\n" +
            $"Текущий баланс: {issueResult.Value.BalanceValue}")
            .NavigateButton<IssueMetricSelectScreen>("Выдать ещё")
            .Row()
            .NavigateButton<BrandWorkspaceScreen>("К бренду")
            .Row()
            .MenuButton("Главное меню"));
    }

    private static Task<IEndpointResult> CancelAsync(UpdateContext ctx)
    {
        ClearIssueSession(ctx);

        return Task.FromResult(BotResults.ShowView(new ScreenView("Выдача метрики отменена.")
            .NavigateButton<BrandWorkspaceScreen>("К бренду")
            .Row()
            .MenuButton("Главное меню")));
    }

    private static void ClearIssueSession(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.MetricDefinitionId);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.MetricName);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.RecipientUserId);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.RecipientCustomerCode);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.Amount);
        ctx.Session?.Data.Remove(IssueMetricSessionKeys.Comment);
    }

    private static string FormatErrors(IReadOnlyCollection<IError> errors)
    {
        var messages = errors
            .Select(error => TranslateError(error.Message))
            .Distinct()
            .ToArray();

        return WebUtility.HtmlEncode(string.Join("; ", messages));
    }

    private static string TranslateError(string message)
    {
        return message switch
        {
            "Access denied" => "нет прав на выдачу метрики",
            "Metric not found" => "метрика не найдена",
            "Brand not found" => "бренд не найден",
            "User not found" => "получатель не найден",
            "Metric is not active" => "метрика неактивна",
            _ => message
        };
    }
}
