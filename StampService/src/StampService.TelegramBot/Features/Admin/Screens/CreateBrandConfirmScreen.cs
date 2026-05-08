using System.Net;
using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class CreateBrandConfirmScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(AdminSessionKeys.CreateBrandName) ?? "-";
        var ownerCode = ctx.Session?.Data.GetString(AdminSessionKeys.CreateOwnerCustomerCode) ?? "-";

        return ValueTask.FromResult(new ScreenView(
            "<b>Создать бренд?</b>\n\n" +
            $"Бренд: {Html(brandName)}\n" +
            $"Владелец: <code>{Html(ownerCode)}</code>")
            .Button<ConfirmCreateBrandAction>("✅ Подтвердить")
            .Row()
            .Button<CancelCreateBrandAction>("❌ Отмена"));
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
