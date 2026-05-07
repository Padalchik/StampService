using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Представляет один шаг визарда.
/// </summary>
/// <typeparam name="TState">Тип данных состояния визарда.</typeparam>
public sealed class WizardStep<TState> where TState : class, new()
{
    /// <summary>
    /// Идентификатор шага.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Функция отрисовки интерфейса шага.
    /// Возвращает <see cref="ScreenView"/> (сообщение и кнопки).
    /// </summary>
    public Func<UpdateContext, TState, Task<ScreenView>> Renderer { get; }

    /// <summary>
    /// Функция обработки пользовательского ввода (Text, Callback, Media).
    /// Возвращает результат маршрутизации <see cref="StepResult"/>.
    /// </summary>
    public Func<UpdateContext, TState, Task<StepResult>> Processor { get; }

    /// <summary>
    /// Функция инициализации шага. 
    /// Вызывается только один раз при переходе на шаг (до первого Render).
    /// </summary>
    public Func<UpdateContext, TState, Task>? OnEnter { get; }

    public WizardStep(
        string id,
        Func<UpdateContext, TState, Task<ScreenView>> renderer,
        Func<UpdateContext, TState, Task<StepResult>> processor,
        Func<UpdateContext, TState, Task>? onEnter = null)
    {
        Id = id;
        Renderer = renderer;
        Processor = processor;
        OnEnter = onEnter;
    }
}