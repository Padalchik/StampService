using System.Net;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.CustomerBalances.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CustomerBalances.Screens;

public sealed class CustomerBalancesPhoneScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";

        return ValueTask.FromResult(new ScreenView(
            $"<b>{Html(brandName)}</b>\n\n" +
            "Введите телефон клиента в международном формате, например +7 999 123-45-67:")
            .AwaitInput<EnterCustomerBalancesPhoneAction>()
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
