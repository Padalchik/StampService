using StampService.Application.Abstractions;
using StampService.Application.Metrics.Queries.GetBrandRedeemMetrics;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.RedeemMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.RedeemMetric.Screens;

public sealed class RedeemMetricSelectScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<IReadOnlyCollection<MetricResponse>, GetBrandRedeemMetricsQuery> _metricsHandler;

    public RedeemMetricSelectScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<IReadOnlyCollection<MetricResponse>, GetBrandRedeemMetricsQuery> metricsHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _metricsHandler = metricsHandler;
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

        var metricsResult = await _metricsHandler.Handle(
            new GetBrandRedeemMetricsQuery(userResult.Value.UserId, brandId),
            ctx.CancellationToken);

        if (metricsResult.IsFailed)
            return new ScreenView("Нет доступа к списанию метрик.").BackButton();

        if (metricsResult.Value.Count == 0)
        {
            return new ScreenView(
                "<b>Списать метрику</b>\n\n" +
                "В этом бренде нет активных метрик.")
                .BackButton();
        }

        var view = new ScreenView("<b>Списать метрику</b>\n\nВыберите метрику:");
        foreach (var metric in metricsResult.Value)
        {
            view.Row().Button<SelectRedeemMetricAction, SelectRedeemMetricPayload>(
                metric.Name,
                new SelectRedeemMetricPayload(metric.Id, metric.Name));
        }

        return view.BackButton();
    }
}
