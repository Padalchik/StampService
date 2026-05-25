using System.Net;
using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class AdminBrandDetailsScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(AdminSessionKeys.SelectedBrandName) ?? "бренд";
        var isMetricsEnabled = ctx.Session?.Data.Get<bool>(AdminSessionKeys.SelectedBrandMetricsEnabled) ?? true;
        var isCoinsEnabled = ctx.Session?.Data.Get<bool>(AdminSessionKeys.SelectedBrandCoinsEnabled) ?? true;
        var ownerName = ctx.Session?.Data.GetString(AdminSessionKeys.SelectedOwnerName);
        var ownerPhoneNumber = ctx.Session?.Data.GetString(AdminSessionKeys.SelectedOwnerPhoneNumber);

        var ownerText = string.IsNullOrWhiteSpace(ownerPhoneNumber)
            ? string.IsNullOrWhiteSpace(ownerName) ? "не назначен" : Html(ownerName)
            : $"{Html(ownerName ?? "пользователь")} · <code>{Html(ownerPhoneNumber)}</code>";

        return ValueTask.FromResult(new ScreenView(
            $"<b>{Html(brandName)}</b>\n\n" +
            $"Владелец: {ownerText}\n" +
            $"Метрики: {FormatEnabled(isMetricsEnabled)}\n" +
            $"Монетки: {FormatEnabled(isCoinsEnabled)}")
            .Button<StartReassignOwnerAction>("Назначить владельца")
            .Row()
            .MenuButton("В главное меню"));
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string FormatEnabled(bool value) => value ? "включены" : "выключены";
}
