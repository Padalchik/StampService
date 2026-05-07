using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// DTO для ответа от визарда к middleware, определяющий дальнейшее поведение.
/// </summary>
/// <param name="IsFinished">Визард завершён — удалить из store.</param>
/// <param name="EndpointResult">Результат для исполнения после обработки шага.</param>
/// <param name="ShouldDeleteUserMessage">
/// Middleware должен удалить входящее текстовое сообщение пользователя.
/// Решение принимает визард, а не middleware (отсутствует инспекция типа результата).
/// </param>
/// <param name="WasCancelled">
/// Indicates the wizard was cancelled (e.g. GoBack from first step) rather than completed successfully.
/// Middleware uses this to invoke <see cref="IBotWizard.OnCancelledAsync"/> before cleanup.
/// </param>
public sealed record WizardTransition(
    bool IsFinished,
    IEndpointResult? EndpointResult = null,
    bool ShouldDeleteUserMessage = false,
    bool WasCancelled = false);

/// <summary>
/// Общий контракт для всех визардов, независимый от типа состояния.
/// Используется middleware для обработки апдейтов без использования рефлексии.
/// </summary>
public interface IBotWizard
{
    /// <summary>
    /// Принимает сырой стейт из хранилища, обрабатывает апдейт и возвращает команду для middleware.
    /// Состояние <paramref name="storageState"/> может быть изменено внутри метода.
    /// </summary>
    Task<WizardTransition> ProcessUpdateAsync(UpdateContext context, WizardStorageState storageState);

    /// <summary>
    /// Инициализирует и запускает первый шаг визарда.
    /// </summary>
    Task<WizardTransition> InitializeAsync(UpdateContext context, WizardStorageState storageState);

    /// <summary>
    /// Возвращается на предыдущий шаг визарда (pop истории шагов).
    /// Если история пуста (текущий шаг — первый), возвращает переход с
    /// <see cref="WizardTransition.IsFinished"/> = <see langword="true"/> и
    /// результатом <c>BotResults.Back()</c> для выхода из визарда.
    /// </summary>
    Task<WizardTransition> GoBackAsync(UpdateContext context, WizardStorageState storageState);

    /// <summary>
    /// Called when the wizard is cancelled (e.g. via /cancel, nav:menu, or back from first step).
    /// Override to perform cleanup.
    /// </summary>
    Task OnCancelledAsync(UpdateContext context, WizardStorageState state);
}