using System.Reflection;

namespace TelegramBotFlow.Core.Endpoints;

/// <summary>
/// Resolves the action ID from a type, respecting ActionIdAttribute if present.
/// </summary>
public static class ActionIdResolver
{
    public static string GetId<TAction>() where TAction : IBotAction =>
        GetId(typeof(TAction));

    public static string GetId(Type actionType)
    {
        var attr = actionType.GetCustomAttribute<ActionIdAttribute>();
        return attr?.Id ?? actionType.Name;
    }
}
