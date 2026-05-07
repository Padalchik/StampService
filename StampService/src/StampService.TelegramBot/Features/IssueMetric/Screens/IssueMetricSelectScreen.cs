using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Queries.GetBrandIssueMetrics;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.IssueMetric.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.IssueMetric.Screens;

public sealed class IssueMetricSelectScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<IReadOnlyCollection<MetricResponse>, GetBrandIssueMetricsQuery> _metricsHandler;

    public IssueMetricSelectScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<IReadOnlyCollection<MetricResponse>, GetBrandIssueMetricsQuery> metricsHandler)
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
            new GetBrandIssueMetricsQuery(userResult.Value.UserId, brandId),
            ctx.CancellationToken);

        if (metricsResult.IsFailed)
            return new ScreenView("Нет доступа к выдаче метрик.").BackButton();

        if (metricsResult.Value.Count == 0)
        {
            return new ScreenView(
                "<b>Выдать метрику</b>\n\n" +
                "В этом бренде нет активных метрик.")
                .BackButton();
        }

        var view = new ScreenView("<b>Выдать метрику</b>\n\nВыберите метрику:");
        foreach (var metric in metricsResult.Value)
        {
            view.Row().Button<SelectIssueMetricAction, SelectIssueMetricPayload>(
                metric.Name,
                new SelectIssueMetricPayload(metric.Id, metric.Name));
        }

        return view.BackButton();
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
