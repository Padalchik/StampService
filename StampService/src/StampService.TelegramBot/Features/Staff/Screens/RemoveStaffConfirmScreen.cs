using System.Net;
using StampService.TelegramBot.Features.Staff.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Staff.Screens;

public sealed class RemoveStaffConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var name = ctx.Session?.Data.GetString(StaffSessionKeys.SelectedStaffName) ?? "сотрудник";
        var phoneNumber = ctx.Session?.Data.GetString(StaffSessionKeys.SelectedStaffPhoneNumber) ?? "-";

        return ValueTask.FromResult(new ScreenView(
            "<b>Удалить сотрудника?</b>\n\n" +
            $"{Html(name)} · <code>{Html(phoneNumber)}</code>\n\n" +
            "Пользователь потеряет доступ к этому бренду как сотрудник.")
            .Button<ConfirmRemoveStaffAction>("✅ Подтвердить")
            .Row()
            .Button<CancelRemoveStaffAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
