using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Запускает визард заданного типа: инициализирует его первый шаг и сохраняет состояние.
/// Возвращает первый <see cref="IEndpointResult"/> для отображения (обычно — экран первого шага).
///
/// Введён для того, чтобы <c>StartWizardResult</c> не зависел напрямую от
/// <c>WizardRegistry</c> и <c>IWizardStore</c> (нарушение слоевых зависимостей).
/// </summary>
public interface IWizardLauncher
{
    /// <summary>
    /// Запускает визард типа <paramref name="wizardType"/> для текущего пользователя.
    /// </summary>
    /// <param name="wizardType">Тип визарда (должен быть зарегистрирован через <c>AddWizards</c>).</param>
    /// <param name="context">Контекст входящего update.</param>
    /// <returns>Первый результат визарда для отображения, или <see langword="null"/>.</returns>
    Task<IEndpointResult?> LaunchAsync(Type wizardType, UpdateContext context);
}