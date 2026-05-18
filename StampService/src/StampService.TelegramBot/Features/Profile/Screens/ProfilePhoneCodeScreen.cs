using System.Net;
using StampService.TelegramBot.Features.Profile.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Profile.Screens;

public sealed class ProfilePhoneCodeScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var phoneNumber = ctx.Session?.Data.GetString(ProfileSessionKeys.PhoneNumber) ?? "телефон";

        return ValueTask.FromResult(new ScreenView(
            "<b>Подтверждение телефона</b>\n\n" +
            $"Код отправлен для {Html(phoneNumber)}.\n" +
            "Введите 6-значный код.")
            .AwaitInput<EnterProfilePhoneCodeAction>()
            .BackButton());
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
