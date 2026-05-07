namespace TelegramBotFlow.Core.Endpoints;

/// <summary>
/// Overrides the default action ID (type name) with an explicit identifier.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ActionIdAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}
