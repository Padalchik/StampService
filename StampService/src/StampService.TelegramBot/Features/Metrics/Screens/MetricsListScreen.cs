using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Queries.GetBrandManageMetrics;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.Metrics.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Metrics.Screens;

public sealed class MetricsListScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<IReadOnlyCollection<MetricResponse>, GetBrandManageMetricsQuery> _metricsHandler;

    public MetricsListScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<IReadOnlyCollection<MetricResponse>, GetBrandManageMetricsQuery> metricsHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _metricsHandler = metricsHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandId = ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        if (brandId == Guid.Empty)
            return new ScreenView("Бренд не выбран.").BackButton();

        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var userResult = await _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(ctx.UserId, from?.FirstName, from?.LastName, from?.Username),
            ctx.CancellationToken);

        if (userResult.IsFailed)
            return new ScreenView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}").BackButton();

        var metricsResult = await _metricsHandler.Handle(
            new GetBrandManageMetricsQuery(userResult.Value.UserId, brandId),
            ctx.CancellationToken);

        if (metricsResult.IsFailed)
            return new ScreenView($"Не удалось загрузить метрики: {BotErrorFormatter.Format(metricsResult.Errors)}").BackButton();

        var view = new ScreenView(
            $"<b>Метрики</b>\n{Html(brandName)}\n\n" +
            (metricsResult.Value.Count == 0
                ? "Метрик пока нет."
                : "Выберите метрику:"));

        foreach (var metric in metricsResult.Value)
        {
            var status = metric.IsActive ? "" : " · выкл.";
            view.Row().Button<OpenMetricDetailsAction, OpenMetricDetailsPayload>(
                $"{metric.Name}{status}",
                new OpenMetricDetailsPayload(metric.Id));
        }

        return view.Row()
            .Button<StartCreateMetricAction>("➕ Создать новую метрику")
            .BackButton();
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
