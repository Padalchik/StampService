namespace TelegramBotFlow.Core.Sessions;

/// <summary>
/// Абстракция хранилища пользовательских сессий.
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Возвращает существующую сессию пользователя или создаёт новую.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя Telegram.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Сессия пользователя.</returns>
    Task<UserSession> GetOrCreateAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет состояние сессии пользователя.
    /// </summary>
    /// <param name="session">Сессия для сохранения.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task SaveAsync(UserSession session, CancellationToken cancellationToken = default);
}