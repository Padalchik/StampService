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
        var ownerName = ctx.Session?.Data.GetString(AdminSessionKeys.SelectedOwnerName);
        var ownerCode = ctx.Session?.Data.GetString(AdminSessionKeys.SelectedOwnerCustomerCode);

        var ownerText = string.IsNullOrWhiteSpace(ownerCode)
            ? "не назначен"
            : $"{Html(ownerName ?? "пользователь")} · <code>{Html(ownerCode)}</code>";

        return ValueTask.FromResult(new ScreenView(
            $"<b>{Html(brandName)}</b>\n\n" +
            $"Владелец: {ownerText}")
            .Button<StartReassignOwnerAction>("Назначить владельца")
            .Row()
            .NavigateButton<AdminPanelScreen>("К брендам")
            .BackButton());
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
