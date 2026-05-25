using System.Net;
using StampService.TelegramBot.Features.Staff.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Staff.Screens;

public sealed class AddStaffPhoneScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        return ValueTask.FromResult(new ScreenView(
            $"<b>{Html(StaffBrandContext.GetBrandName(ctx))}</b>\n\n" +
            "Введите номер телефона сотрудника:")
            .AwaitInput<EnterAddStaffPhoneAction>()
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
