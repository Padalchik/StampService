using System.Net;
using StampService.TelegramBot.Features.Brands.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Brands.Screens;

public sealed class BrandSettingsScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var isMetricsEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.EditMetricsEnabledSessionKey)
            ?? ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsMetricsEnabledSessionKey)
            ?? true;
        var isCoinsEnabled = ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.EditCoinsEnabledSessionKey)
            ?? ctx.Session?.Data.Get<bool>(BrandWorkspaceScreen.IsCoinsEnabledSessionKey)
            ?? true;

        var view = new ScreenView(
            "<b>Настройки бренда</b>\n\n" +
            $"{Html(brandName)}\n" +
            $"Метрики: {FormatEnabled(isMetricsEnabled)}\n" +
            $"Монетки: {FormatEnabled(isCoinsEnabled)}\n\n" +
            "Должен быть включён хотя бы один тип.");

        view.Button<ToggleBrandSettingsMetricsAction>($"{ToggleMarker(isMetricsEnabled)} Учитывать метрики");
        view.Row().Button<ToggleBrandSettingsCoinsAction>($"{ToggleMarker(isCoinsEnabled)} Учитывать монетки");

        if (isMetricsEnabled || isCoinsEnabled)
            view.Row().Button<ConfirmBrandSettingsAction>("✅ Сохранить");

        return ValueTask.FromResult(view
            .Row()
            .Button<CancelBrandSettingsAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static string FormatEnabled(bool value) => value ? "включены" : "выключены";

    private static string ToggleMarker(bool value) => value ? "✅" : "⬜";
}
