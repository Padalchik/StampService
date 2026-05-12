using System.Net;
using StampService.TelegramBot.Features.Metrics.Actions;
using StampService.TelegramBot.Features.Metrics.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Brands.Screens;

public sealed class MetricWorkScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var canManageMetrics = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.CanManageMetricsSessionKey) ?? false;

        if (!canManageMetrics)
        {
            return ValueTask.FromResult(new ScreenView(
                $"<b>{Html(brandName)}</b>\n\n" +
                "Нет доступа к управлению метриками.")
                .BackButton());
        }

        return ValueTask.FromResult(new ScreenView(
            $"<b>{Html(brandName)}</b>\n\n" +
            "Работа с метриками")
            .NavigateButton<MetricsListScreen>("Все метрики")
            .Row()
            .Button<StartCreateMetricAction>("Создать метрику")
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
