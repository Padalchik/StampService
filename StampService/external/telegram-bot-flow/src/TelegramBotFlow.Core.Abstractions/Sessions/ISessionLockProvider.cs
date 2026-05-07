namespace TelegramBotFlow.Core.Sessions;

/// <summary>
/// Обеспечивает эксклюзивный доступ к сессии пользователя для предотвращения состояния гонки.
/// </summary>
public interface ISessionLockProvider
{
    /// <summary>
    /// Захватывает блокировку сессии для указанного пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Объект блокировки, который необходимо освободить через Dispose().</returns>
    /// <exception cref="TimeoutException">Выбрасывается, если не удалось захватить блокировку за отведённое время.</exception>
    Task<IDisposable> AcquireLockAsync(long userId, CancellationToken cancellationToken = default);
}