using System.Net;
using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class ReassignOwnerCodeScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(AdminSessionKeys.SelectedBrandName) ?? "бренд";
        return ValueTask.FromResult(new ScreenView(
            $"<b>{Html(brandName)}</b>\n\n" +
            "Введите CustomerCode нового владельца:")
            .AwaitInput<EnterReassignOwnerCodeAction>()
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
