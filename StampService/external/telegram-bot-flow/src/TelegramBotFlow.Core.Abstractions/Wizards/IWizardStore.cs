namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Интерфейс хранилища состояний визардов.
/// </summary>
public interface IWizardStore
{
    /// <summary>
    /// Получает состояние визарда.
    /// </summary>
    /// <param name="userId">ID пользователя Telegram.</param>
    /// <param name="wizardId">ID визарда.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Состояние визарда или <see langword="null"/>, если не найдено.</returns>
    Task<WizardStorageState?> GetAsync(long userId, string wizardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет состояние визарда.
    /// </summary>
    /// <param name="userId">ID пользователя Telegram.</param>
    /// <param name="wizardId">ID визарда.</param>
    /// <param name="state">Состояние визарда.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task SaveAsync(long userId, string wizardId, WizardStorageState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет состояние визарда.
    /// </summary>
    /// <param name="userId">ID пользователя Telegram.</param>
    /// <param name="wizardId">ID визарда.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task DeleteAsync(long userId, string wizardId, CancellationToken cancellationToken = default);
}