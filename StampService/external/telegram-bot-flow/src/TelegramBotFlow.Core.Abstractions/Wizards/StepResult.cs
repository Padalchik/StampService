namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Результат обработки шага визарда.
/// </summary>
public abstract record StepResult
{
    private StepResult() { }

    /// <summary>
    /// Остаться на текущем шаге.
    /// Опционально может быть указан текст ошибки (notification), который покажется пользователю как AnswerCallback.
    /// </summary>
    public sealed record StayResult(string? Notification = null) : StepResult;

    /// <summary>
    /// Перейти к другому шагу по его ID.
    /// </summary>
    public sealed record GoToResult(string StepId) : StepResult;

    /// <summary>
    /// Успешно завершить визард.
    /// </summary>
    public sealed record FinishResult : StepResult;

    /// <summary>
    /// Вернуться на предыдущий шаг визарда (pop истории шагов).
    /// Если история пуста (текущий шаг — первый), визард завершается и выполняется навигация назад.
    /// </summary>
    public sealed record GoBackResult : StepResult;

    /// <summary>
    /// Остаться на текущем шаге.
    /// </summary>
    public static StayResult Stay(string? notification = null) => new(notification);

    /// <summary>
    /// Перейти к шагу <paramref name="stepId"/>.
    /// </summary>
    public static GoToResult GoTo(string stepId) => new(stepId);

    /// <summary>
    /// Завершить визард.
    /// </summary>
    public static FinishResult Finish() => new();

    /// <summary>
    /// Вернуться на предыдущий шаг визарда.
    /// </summary>
    public static GoBackResult GoBack() => new();
}