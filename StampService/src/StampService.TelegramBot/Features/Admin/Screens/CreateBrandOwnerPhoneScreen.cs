using System.Net;
using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class CreateBrandOwnerPhoneScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(AdminSessionKeys.CreateBrandName) ?? "бренд";
        return ValueTask.FromResult(new ScreenView(
            $"<b>{Html(brandName)}</b>\n\n" +
            "Введите телефон владельца:")
            .AwaitInput<EnterCreateBrandOwnerPhoneAction>()
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
