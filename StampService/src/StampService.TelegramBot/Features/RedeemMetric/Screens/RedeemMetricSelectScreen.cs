using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Queries.GetRedeemMetricOptions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.RedeemMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.RedeemMetric.Screens;

public sealed class RedeemMetricSelectScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<RedeemMetricOptionsResponse, GetRedeemMetricOptionsQuery> _optionsHandler;

    public RedeemMetricSelectScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<RedeemMetricOptionsResponse, GetRedeemMetricOptionsQuery> optionsHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _optionsHandler = optionsHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandId = ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
        if (brandId == Guid.Empty)
            return new ScreenView("Бренд не выбран.").BackButton();

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

        var redemptionCode = ctx.Session?.Data.GetString(RedeemMetricSessionKeys.RedemptionCode) ?? string.Empty;
        var optionsResult = await _optionsHandler.Handle(
            new GetRedeemMetricOptionsQuery(userResult.Value.UserId, brandId, redemptionCode),
            ctx.CancellationToken);

        if (optionsResult.IsFailed)
            return new ScreenView($"Не удалось загрузить метрики для списания: {BotErrorFormatter.Format(optionsResult.Errors, BotErrorContext.RedeemMetric)}").BackButton();

        ctx.Session?.Data.Set(RedeemMetricSessionKeys.CustomerUserId, optionsResult.Value.CustomerUserId);
        ctx.Session?.Data.Set(RedeemMetricSessionKeys.CustomerName, optionsResult.Value.CustomerName);

        if (optionsResult.Value.Metrics.Count == 0)
        {
            return new ScreenView(
                "<b>Списать метрику</b>\n\n" +
                "В этом бренде нет активных метрик.")
                .BackButton();
        }

        var view = new ScreenView(
            "<b>Списать метрику</b>\n\n" +
            $"Клиент: {Html(optionsResult.Value.CustomerName)}\n" +
            $"Код списания клиента: <code>{Html(optionsResult.Value.RedemptionCode)}</code>\n\n" +
            "Выберите метрику:");

        foreach (var metric in optionsResult.Value.Metrics)
        {
            var marker = metric.CanRedeem ? "✅" : "⛔️";
            view.Row().Button<SelectRedeemMetricAction, SelectRedeemMetricPayload>(
                $"{marker} {metric.MetricName} {metric.CurrentBalance}/{metric.RequiredAmount}",
                new SelectRedeemMetricPayload(
                    metric.MetricDefinitionId,
                    metric.MetricName,
                    metric.RequiredAmount,
                    metric.CurrentBalance,
                    metric.CanRedeem));
        }

        return view.BackButton();
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
