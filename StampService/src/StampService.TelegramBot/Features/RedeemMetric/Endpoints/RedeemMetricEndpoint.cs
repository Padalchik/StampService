using System.Net;
using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Commands.RedeemMetric;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
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
        ctx.Session?.Data.Set(RedeemMetricSessionKeys.MetricDefinitionId, payload.MetricDefinitionId);
        ctx.Session?.Data.Set(RedeemMetricSessionKeys.MetricName, payload.MetricName);
        ctx.Session?.Data.Set(RedeemMetricSessionKeys.RedemptionAmount, payload.RedemptionAmount);

        return Task.FromResult(BotResults.NavigateTo<RedeemMetricCodeScreen>());
    }

    private static async Task<IEndpointResult> EnterCodeAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IRedeemMetricValidationService redeemMetricValidationService)
    {
        var metricDefinitionId = ctx.Session?.Data.Get<Guid>(RedeemMetricSessionKeys.MetricDefinitionId) ?? Guid.Empty;
        if (metricDefinitionId == Guid.Empty)
            return BotResults.ShowView(new ScreenView("РЎС†РµРЅР°СЂРёР№ СЃРїРёСЃР°РЅРёСЏ СѓСЃС‚Р°СЂРµР». РќР°С‡РЅРёС‚Рµ Р·Р°РЅРѕРІРѕ.").BackButton());

        var code = ctx.MessageText?.Trim() ?? string.Empty;
        if (!DomainRedemptionCode.IsValidCode(code))
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                "Код для списания должен состоять из 6 цифр. Попробуйте ещё раз.")
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
                $"РќРµ СѓРґР°Р»РѕСЃСЊ РѕРїСЂРµРґРµР»РёС‚СЊ СЃРѕС‚СЂСѓРґРЅРёРєР°: {FormatErrors(redeemerResult.Errors)}")
                .AwaitInput<EnterRedeemCodeAction>()
                .BackButton()));
        }

        var precheckResult = await redeemMetricValidationService.ValidateAsync(
            metricDefinitionId,
            redeemerResult.Value.UserId,
            code,
            ctx.CancellationToken);

        if (precheckResult.IsFailed)
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                $"Нельзя списать метрику: {FormatErrors(precheckResult.Errors)}")
                .AwaitInput<EnterRedeemCodeAction>()
                .BackButton()));
        }

        ctx.Session?.Data.Set(RedeemMetricSessionKeys.RedemptionCode, code);

        return BotInputResults.DeleteInputThen(BotResults.NavigateTo<RedeemMetricCommentScreen>());
    }

    private static async Task<IEndpointResult> EnterCommentAsync(UpdateContext ctx)
    {
        var comment = ctx.MessageText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(comment))
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                "Комментарий обязателен. Попробуйте ещё раз.")
                .AwaitInput<EnterRedeemCommentAction>()
                .BackButton()));
        }

        if (comment.Length > LoyaltyConstants.MAX_TRANSACTION_COMMENT_LENGTH)
        {
            return BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(
                $"Комментарий не должен быть длиннее {LoyaltyConstants.MAX_TRANSACTION_COMMENT_LENGTH} символов. Попробуйте ещё раз.")
                .AwaitInput<EnterRedeemCommentAction>()
                .BackButton()));
        }

        ctx.Session?.Data.Set(RedeemMetricSessionKeys.Comment, comment);

        return BotInputResults.DeleteInputThen(BotResults.NavigateTo<RedeemMetricConfirmScreen>());
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
                $"Не удалось определить сотрудника: {FormatErrors(redeemerResult.Errors)}")
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
                $"Не удалось списать метрику: {FormatErrors(redeemResult.Errors)}")
                .BackButton());
        }

        ClearRedeemSession(ctx);

        return BotResults.ShowView(new ScreenView(
            "<b>Метрика списана</b>\n\n" +
            $"Количество: {redeemResult.Value.Amount}\n" +
            $"Текущий баланс: {redeemResult.Value.BalanceValue}")
            .NavigateButton<RedeemMetricSelectScreen>("Списать ещё")
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
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.MetricDefinitionId);
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.MetricName);
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.RedemptionAmount);
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.RedemptionCode);
        ctx.Session?.Data.Remove(RedeemMetricSessionKeys.Comment);
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
        if (message.StartsWith("Insufficient metric balance. Current:", StringComparison.Ordinal))
        {
            var currentStart = message.IndexOf("Current: ", StringComparison.Ordinal);
            var requiredStart = message.IndexOf(", required: ", StringComparison.Ordinal);
            if (currentStart >= 0 && requiredStart > currentStart)
            {
                var current = message.Substring(currentStart + "Current: ".Length, requiredStart - (currentStart + "Current: ".Length));
                var required = message.Substring(requiredStart + ", required: ".Length);
                return $"недостаточно баланса для списания ({current}/{required})";
            }

            return "недостаточно баланса для списания";
        }

        return message switch
        {
            "Access denied" => "нет прав на списание метрики",
            "Metric not found" => "метрика не найдена",
            "Brand not found" => "бренд не найден",
            "Metric is not active" => "метрика неактивна",
            "Redemption code must contain exactly 6 digits" => "код списания должен состоять из 6 цифр",
            "Redemption code not found or expired" => "код списания не найден или истёк",
            "Redemption code has already been used" => "код списания уже использован",
            "Metric balance not found" => "у клиента нет баланса по этой метрике",
            _ => message
        };
    }
}


