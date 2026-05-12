using StampService.Application.Abstractions;
using StampService.Application.Metrics.Commands.RedeemMetric;
using StampService.Application.Metrics.Queries.GetRedeemMetricOptions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Routing;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.RedeemMetric.Actions;
using StampService.TelegramBot.Features.RedeemMetric.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using DomainRedemptionCode = StampService.Domain.User.RedemptionCode;
using LoyaltyConstants = StampService.Domain.Loyalty.Constants;

namespace StampService.TelegramBot.Features.RedeemMetric.Endpoints;

public sealed class RedeemMetricEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<SelectRedeemMetricAction, SelectRedeemMetricPayload>(SelectMetricAsync);
        app.MapInput<EnterRedeemCodeAction>(EnterCodeAsync);
        app.MapInput<EnterRedeemCommentAction>(EnterCommentAsync);
        app.MapAction<ConfirmRedeemMetricAction>(ConfirmAsync);
        app.MapAction<CancelRedeemMetricAction>(CancelAsync);
    }

    private static Task<IEndpointResult> SelectMetricAsync(
        UpdateContext ctx,
        SelectRedeemMetricPayload payload)
    {
        if (!payload.CanRedeem)
            return Task.FromResult(BotResults.NavigateTo<RedeemMetricSelectScreen>());

        ctx.Session?.Data.Set(RedeemMetricSessionKeys.MetricDefinitionId, payload.MetricDefinitionId);
        ctx.Session?.Data.Set(RedeemMetricSessionKeys.MetricName, payload.MetricName);
        ctx.Session?.Data.Set(RedeemMetricSessionKeys.RedemptionAmount, payload.RedemptionAmount);
        ctx.Session?.Data.Set(RedeemMetricSessionKeys.CurrentBalance, payload.CurrentBalance);

        return Task.FromResult(BotResults.NavigateTo<RedeemMetricCommentScreen>());
    }

    private static async Task<IEndpointResult> EnterCodeAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<RedeemMetricOptionsResponse, GetRedeemMetricOptionsQuery> optionsHandler)
    {
        var brandId = ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
        if (brandId == Guid.Empty)
            return BotResults.ShowView(new ScreenView("Бренд не выбран.").BackButton());

        var code = ctx.MessageText?.Trim() ?? string.Empty;
        if (!DomainRedemptionCode.IsValidCode(code))
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                "Код списания должен состоять из 4 цифр. Попробуйте еще раз.")
                .AwaitInput<EnterRedeemCodeAction>()
                .BackButton()));
        }

        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var redeemerResult = await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (redeemerResult.IsFailed)
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                $"Не удалось определить сотрудника: {BotErrorFormatter.Format(redeemerResult.Errors, BotErrorContext.RedeemMetric)}")
                .AwaitInput<EnterRedeemCodeAction>()
                .BackButton()));
        }

        var optionsResult = await optionsHandler.Handle(
            new GetRedeemMetricOptionsQuery(
                redeemerResult.Value.UserId,
                brandId,
                code),
            ctx.CancellationToken);

        if (optionsResult.IsFailed)
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                $"Нельзя начать списание: {BotErrorFormatter.Format(optionsResult.Errors, BotErrorContext.RedeemMetric)}")
                .AwaitInput<EnterRedeemCodeAction>()
                .BackButton()));
        }

        ClearSelectedMetric(ctx);
        ctx.Session?.Data.Set(RedeemMetricSessionKeys.RedemptionCode, code);
        ctx.Session?.Data.Set(RedeemMetricSessionKeys.CustomerUserId, optionsResult.Value.CustomerUserId);
        ctx.Session?.Data.Set(RedeemMetricSessionKeys.CustomerName, optionsResult.Value.CustomerName);

        return BotInputResults.DeleteInputThen(BotResults.NavigateTo<RedeemMetricSelectScreen>());
    }

    private static Task<IEndpointResult> EnterCommentAsync(UpdateContext ctx)
    {
        var comment = ctx.MessageText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(comment))
        {
            return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                "Комментарий обязателен. Попробуйте еще раз.")
                .AwaitInput<EnterRedeemCommentAction>()
                .BackButton())));
        }

        if (comment.Length > LoyaltyConstants.MAX_TRANSACTION_COMMENT_LENGTH)
        {
            return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                $"Комментарий не должен быть длиннее {LoyaltyConstants.MAX_TRANSACTION_COMMENT_LENGTH} символов. Попробуйте еще раз.")
                .AwaitInput<EnterRedeemCommentAction>()
                .BackButton())));
        }

        ctx.Session?.Data.Set(RedeemMetricSessionKeys.Comment, comment);

        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<RedeemMetricConfirmScreen>()));
    }

    private static async Task<IEndpointResult> ConfirmAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<RedeemMetricResponse, RedeemMetricCommand> redeemMetricHandler)
    {
        var metricDefinitionId = ctx.Session?.Data.Get<Guid>(RedeemMetricSessionKeys.MetricDefinitionId) ?? Guid.Empty;
        var redemptionAmount = ctx.Session?.Data.Get<int>(RedeemMetricSessionKeys.RedemptionAmount) ?? 0;
        var redemptionCode = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.RedemptionCode) ?? string.Empty;
        var comment = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.Comment) ?? string.Empty;

        if (metricDefinitionId == Guid.Empty
            || redemptionAmount <= 0
            || !DomainRedemptionCode.IsValidCode(redemptionCode)
            || string.IsNullOrWhiteSpace(comment))
        {
            return BotResults.ShowView(new ScreenView("Сценарий списания устарел. Начните заново.").BackButton());
        }

        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var redeemerResult = await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (redeemerResult.IsFailed)
        {
            return BotResults.ShowView(new ScreenView(
                $"Не удалось определить сотрудника: {BotErrorFormatter.Format(redeemerResult.Errors, BotErrorContext.RedeemMetric)}")
                .BackButton());
        }

        var redeemResult = await redeemMetricHandler.Handle(
            new RedeemMetricCommand(
                metricDefinitionId,
                redeemerResult.Value.UserId,
                new RedeemMetricRequest(
                    redemptionCode,
                    comment)),
            ctx.CancellationToken);

        if (redeemResult.IsFailed)
        {
            return BotResults.ShowView(new ScreenView(
                $"Не удалось списать метрику: {BotErrorFormatter.Format(redeemResult.Errors, BotErrorContext.RedeemMetric)}")
                .BackButton());
        }

        ClearRedeemSession(ctx);

        return BotResults.ShowView(new ScreenView(
            "<b>Метрика списана</b>\n\n" +
            $"Количество: {redeemResult.Value.Amount}\n" +
            $"Текущий баланс: {redeemResult.Value.BalanceValue}")
            .NavigateButton<RedeemMetricCodeScreen>("Списать еще")
            .Row()
            .NavigateButton<BrandWorkspaceScreen>("К бренду")
            .Row()
            .MenuButton("Главное меню"));
    }

    private static Task<IEndpointResult> CancelAsync(UpdateContext ctx)
    {
        ClearRedeemSession(ctx);

        return Task.FromResult(BotResults.ShowView(new ScreenView("Списание метрики отменено.")
            .NavigateButton<BrandWorkspaceScreen>("К бренду")
            .Row()
            .MenuButton("Главное меню")));
    }

    private static void ClearRedeemSession(UpdateContext ctx)
    {
        ClearSelectedMetric(ctx);
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.RedemptionCode);
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.CustomerUserId);
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.CustomerName);
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.Comment);
    }

    private static void ClearSelectedMetric(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.MetricDefinitionId);
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.MetricName);
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.RedemptionAmount);
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.CurrentBalance);
    }
}
