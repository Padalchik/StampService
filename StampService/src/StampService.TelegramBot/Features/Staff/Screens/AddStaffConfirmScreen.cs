using System.Net;
using StampService.TelegramBot.Features.Staff.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Staff.Screens;

public sealed class AddStaffConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var phoneNumber = ctx.Session?.Data.GetString(StaffSessionKeys.AddPhoneNumber) ?? "-";

        return ValueTask.FromResult(new ScreenView(
            "<b>Добавить сотрудника?</b>\n\n" +
            $"Бренд: {Html(StaffBrandContext.GetBrandName(ctx))}\n" +
            $"Телефон: <code>{Html(phoneNumber)}</code>\n" +
            "Роль: Сотрудник")
            .Button<ConfirmAddStaffAction>("✅ Подтвердить")
            .Row()
            .Button<CancelAddStaffAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
