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
        var currentOwnerCode = ctx.Session?.Data.GetString(AdminSessionKeys.SelectedOwnerCustomerCode);
        var newOwnerCode = ctx.Session?.Data.GetString(AdminSessionKeys.ReassignOwnerCustomerCode) ?? "-";

        var currentOwnerText = string.IsNullOrWhiteSpace(currentOwnerCode)
            ? "не назначен"
            : $"<code>{Html(currentOwnerCode)}</code>";

        return ValueTask.FromResult(new ScreenView(
            "<b>Сменить владельца?</b>\n\n" +
            $"Бренд: {Html(brandName)}\n" +
            $"Текущий владелец: {currentOwnerText}\n" +
            $"Новый владелец: <code>{Html(newOwnerCode)}</code>\n\n" +
            "Старый владелец потеряет membership в бренде.")
            .Button<ConfirmReassignOwnerAction>("Сменить владельца")
            .Row()
            .Button<CancelReassignOwnerAction>("Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
