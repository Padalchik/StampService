using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Metrics.Queries.GetMetricDetails;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Metrics;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.Metrics.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Metrics.Screens;

public sealed class MetricDetailsScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<MetricResponse, GetMetricDetailsQuery> _metricHandler;

    public MetricDetailsScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<MetricResponse, GetMetricDetailsQuery> metricHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _metricHandler = metricHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var metricDefinitionId = ctx.Session?.Data.Get<Guid>(MetricManagementSessionKeys.SelectedMetricDefinitionId) ?? Guid.Empty;
        if (metricDefinitionId == Guid.Empty)
            return new ScreenView("Метрика не выбрана.").BackButton();

        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var userResult = await _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(ctx.UserId, from?.FirstName, from?.LastName, from?.Username),
            ctx.CancellationToken);

        if (userResult.IsFailed)
            return new ScreenView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}").BackButton();

        var metricResult = await _metricHandler.Handle(
            new GetMetricDetailsQuery(userResult.Value.UserId, metricDefinitionId),
            ctx.CancellationToken);

        if (metricResult.IsFailed)
            return new ScreenView($"Не удалось открыть метрику: {BotErrorFormatter.Format(metricResult.Errors)}").BackButton();

        var metric = metricResult.Value;
        StoreMetric(ctx, metric);

        var status = metric.IsActive ? "активна" : "выключена";
        return new ScreenView(
            $"<b>{Html(metric.Name)}</b>\n\n" +
            $"Списание: {metric.RedemptionAmount}\n" +
            $"Статус: {status}\n" +
            $"Создана: {metric.CreatedAt:dd.MM.yyyy HH:mm} UTC")
            .Button<StartEditMetricAction>("Редактировать")
            .Row()
            .NavigateButton<MetricsListScreen>("К метрикам")
            .BackButton();
    }

    private static void StoreMetric(UpdateContext ctx, MetricResponse metric)
    {
        ctx.Session?.Data.Set(MetricManagementSessionKeys.SelectedMetricDefinitionId, metric.Id);
        ctx.Session?.Data.Set(MetricManagementSessionKeys.SelectedMetricName, metric.Name);
        ctx.Session?.Data.Set(MetricManagementSessionKeys.SelectedMetricRedemptionAmount, metric.RedemptionAmount);
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
