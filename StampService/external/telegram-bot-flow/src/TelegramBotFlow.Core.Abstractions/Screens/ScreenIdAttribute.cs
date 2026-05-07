namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Overrides the default screen ID (convention-based) with an explicit identifier.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ScreenIdAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}
