namespace TelegramBotFlow.Core.Context;

/// <summary>
/// Определяет правила административного доступа для текущего пользователя.
/// </summary>
public interface IUserAccessPolicy
{
    /// <summary>
    /// Проверяет, является ли пользователь администратором.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <returns><see langword="true"/>, если пользователь обладает admin-доступом.</returns>
    bool IsAdmin(UpdateContext context);
}