using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Нетипизированное состояние визарда для хранения в <see cref="IWizardStore"/>.
/// </summary>
public sealed class WizardStorageState
{
    /// <summary>
    /// Идентификатор текущего шага.
    /// </summary>
    public string CurrentStepId { get; set; } = string.Empty;

    /// <summary>
    /// История посещённых шагов (стек для GoBack).
    /// Последний элемент — предыдущий шаг.
    /// </summary>
    public List<string> StepHistory { get; set; } = [];

    /// <summary>
    /// Сериализованный payload (JSON).
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// Время создания визарда (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Время истечения срока действия визарда (UTC).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}