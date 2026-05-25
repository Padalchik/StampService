using System.Net;
using StampService.TelegramBot.Features.Staff.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Staff.Screens;

public sealed class StaffDetailsScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var name = ctx.Session?.Data.GetString(StaffSessionKeys.SelectedStaffName) ?? "сотрудник";
        var phoneNumber = ctx.Session?.Data.GetString(StaffSessionKeys.SelectedStaffPhoneNumber) ?? "-";

        return ValueTask.FromResult(new ScreenView(
            $"<b>{Html(name)}</b>\n\n" +
            $"Телефон: <code>{Html(phoneNumber)}</code>\n" +
            "Роль: Сотрудник")
            .Button<StartRemoveStaffAction>("Удалить сотрудника")
            .Row()
            .NavigateButton<BrandStaffListScreen>("К сотрудникам")
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
