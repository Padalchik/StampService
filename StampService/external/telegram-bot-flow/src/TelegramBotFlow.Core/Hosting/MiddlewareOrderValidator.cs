namespace TelegramBotFlow.Core.Hosting;

/// <summary>
/// Validates that session-dependent middlewares are registered after UseSession().
/// Extracted for testability.
/// </summary>
internal static class MiddlewareOrderValidator
{
    public static void Validate(IList<string> registeredMiddleware)
    {
        int sessionIndex = registeredMiddleware.IndexOf("session");

        void RequireSessionBefore(string name)
        {
            int index = registeredMiddleware.IndexOf(name);
            if (index >= 0 && (sessionIndex < 0 || sessionIndex > index))
                throw new InvalidOperationException(
                    $"Use{char.ToUpper(name[0])}{name[1..]}() requires UseSessions() to be registered before it.");
        }

        RequireSessionBefore("wizards");
        RequireSessionBefore("pending_input");
    }
}
