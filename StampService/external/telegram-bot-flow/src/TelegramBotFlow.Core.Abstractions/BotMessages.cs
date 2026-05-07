namespace TelegramBotFlow.Core;

/// <summary>
/// Default UI strings used by the framework. Override via Configure&lt;BotMessages&gt;() for localization.
/// </summary>
public class BotMessages
{
    public string BackButton { get; set; } = "\u2190 Back";
    public string MenuButton { get; set; } = "\u2630 Menu";
    public string CloseButton { get; set; } = "\u2190 Back";
    public string PayloadExpired { get; set; } = "Button data expired. Please refresh the menu.";
    public string ErrorMessage { get; set; } = "An error occurred. Please try again later.";
}
