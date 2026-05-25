using System.Net;
using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class ReassignOwnerConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(AdminSessionKeys.SelectedBrandName) ?? "бренд";
        var currentOwnerName = ctx.Session?.Data.GetString(AdminSessionKeys.SelectedOwnerName);
        var currentOwnerPhoneNumber = ctx.Session?.Data.GetString(AdminSessionKeys.SelectedOwnerPhoneNumber);
        var newOwnerPhoneNumber = ctx.Session?.Data.GetString(AdminSessionKeys.ReassignOwnerPhoneNumber) ?? "-";

        var currentOwnerText = string.IsNullOrWhiteSpace(currentOwnerPhoneNumber)
            ? string.IsNullOrWhiteSpace(currentOwnerName) ? "не назначен" : Html(currentOwnerName)
            : $"<code>{Html(currentOwnerPhoneNumber)}</code>";

        return ValueTask.FromResult(new ScreenView(
            "<b>Сменить владельца?</b>\n\n" +
            $"Бренд: {Html(brandName)}\n" +
            $"Текущий владелец: {currentOwnerText}\n" +
            $"Новый владелец: <code>{Html(newOwnerPhoneNumber)}</code>\n\n" +
            "Старый владелец потеряет membership в бренде.")
            .Button<ConfirmReassignOwnerAction>("✅ Подтвердить")
            .Row()
            .Button<CancelReassignOwnerAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
