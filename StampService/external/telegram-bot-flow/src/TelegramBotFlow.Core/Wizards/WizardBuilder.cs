using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Строитель конфигурации визарда. Позволяет добавлять шаги декларативно.
/// </summary>
/// <typeparam name="TState">Тип состояния визарда.</typeparam>
public sealed class WizardBuilder<TState> where TState : class, new()
{
    private readonly Dictionary<string, WizardStep<TState>> _steps = [];
    private string? _initialStepId;

    /// <summary>
    /// Добавляет шаг в визард.
    /// </summary>
    /// <param name="id">ID шага.</param>
    /// <param name="renderer">Фаза рендера (View).</param>
    /// <param name="processor">Фаза обработки ввода (Logic).</param>
    /// <param name="onEnter">Функция инициализации (срабатывает 1 раз при входе).</param>
    /// <returns>Экземпляр строителя для цепочек вызовов (Fluent API).</returns>
    public WizardBuilder<TState> Step(
        string id,
        Func<UpdateContext, TState, Task<ScreenView>> renderer,
        Func<UpdateContext, TState, Task<StepResult>> processor,
        Func<UpdateContext, TState, Task>? onEnter = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Step ID cannot be empty", nameof(id));

        if (_steps.ContainsKey(id))
            throw new ArgumentException($"Step with ID '{id}' already exists", nameof(id));

        _initialStepId ??= id;

        _steps[id] = new WizardStep<TState>(id, renderer, processor, onEnter);

        return this;
    }

    /// <summary>
    /// Добавляет шаг с синхронным рендером.
    /// </summary>
    public WizardBuilder<TState> Step(
        string id,
        Func<UpdateContext, TState, ScreenView> renderer,
        Func<UpdateContext, TState, Task<StepResult>> processor,
        Func<UpdateContext, TState, Task>? onEnter = null)
    {
        return Step(id, (ctx, s) => Task.FromResult(renderer(ctx, s)), processor, onEnter);
    }

    /// <summary>
    /// Добавляет шаг с синхронным процессором.
    /// </summary>
    public WizardBuilder<TState> Step(
        string id,
        Func<UpdateContext, TState, Task<ScreenView>> renderer,
        Func<UpdateContext, TState, StepResult> processor,
        Func<UpdateContext, TState, Task>? onEnter = null)
    {
        return Step(id, renderer, (ctx, s) => Task.FromResult(processor(ctx, s)), onEnter);
    }

    /// <summary>
    /// Добавляет шаг с синхронным рендером и синхронным процессором.
    /// </summary>
    public WizardBuilder<TState> Step(
        string id,
        Func<UpdateContext, TState, ScreenView> renderer,
        Func<UpdateContext, TState, StepResult> processor,
        Func<UpdateContext, TState, Task>? onEnter = null)
    {
        return Step(
            id,
            (ctx, s) => Task.FromResult(renderer(ctx, s)),
            (ctx, s) => Task.FromResult(processor(ctx, s)),
            onEnter);
    }

    internal IReadOnlyDictionary<string, WizardStep<TState>> Build(out string initialStepId)
    {
        if (_steps.Count == 0)
            throw new InvalidOperationException("Wizard must have at least one step.");

        initialStepId = _initialStepId!;
        return _steps;
    }
}