using System.Text.Json;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Базовый класс для всех визардов (машин состояний).
/// Управляет жизненным циклом (рендеринг, процессинг, роутинг) шагов.
/// </summary>
/// <typeparam name="TState">Тип состояния (DTO для сбора данных).</typeparam>
public abstract class BotWizard<TState> : IBotWizard where TState : class, new()
{
    private IReadOnlyDictionary<string, WizardStep<TState>>? _steps;
    private string? _initialStepId;

    /// <summary>
    /// Конфигурация шагов (Builder pattern).
    /// </summary>
    protected abstract void ConfigureSteps(WizardBuilder<TState> builder);

    /// <summary>
    /// Вызывается после успешного завершения всех шагов визарда.
    /// Должен вернуть результат для роутинга (например, <see cref="NavigateToRootResult"/>).
    /// </summary>
    public abstract Task<IEndpointResult> OnFinishedAsync(UpdateContext context, TState state);

    /// <summary>
    /// Called when the wizard is cancelled. Override to perform cleanup.
    /// </summary>
    protected virtual Task OnCancelledAsync(UpdateContext context, TState state)
        => Task.CompletedTask;

    /// <inheritdoc />
    async Task IBotWizard.OnCancelledAsync(UpdateContext context, WizardStorageState state)
    {
        TState typedState;
        try
        {
            typedState = string.IsNullOrWhiteSpace(state.PayloadJson)
                ? new TState()
                : JsonSerializer.Deserialize<TState>(state.PayloadJson) ?? new TState();
        }
        catch (JsonException)
        {
            typedState = new TState();
        }

        await OnCancelledAsync(context, typedState);
    }

    /// <summary>
    /// Время жизни состояния визарда (опционально).
    /// По умолчанию <see langword="null"/> (никогда не истекает).
    /// </summary>
    protected virtual TimeSpan? ExpiresAfter => null;

    internal WizardStep<TState> GetStep(string stepId)
    {
        EnsureConfigured();

        if (_steps!.TryGetValue(stepId, out WizardStep<TState>? step))
            return step;

        throw new InvalidOperationException($"Step with ID '{stepId}' not found in wizard {GetType().Name}");
    }

    internal WizardStep<TState> GetInitialStep()
    {
        EnsureConfigured();
        return _steps![_initialStepId!];
    }

    internal DateTime? CalculateExpiration() =>
        ExpiresAfter.HasValue ? DateTime.UtcNow.Add(ExpiresAfter.Value) : null;

    public async Task<WizardTransition> InitializeAsync(UpdateContext context, WizardStorageState storageState)
    {
        EnsureConfigured();

        WizardStep<TState> initialStep = _steps![_initialStepId!];
        storageState.CurrentStepId = initialStep.Id;
        storageState.ExpiresAt = CalculateExpiration();

        TState state = new();

        if (initialStep.OnEnter is not null)
        {
            try
            {
                await initialStep.OnEnter(context, state);
            }
            catch (Exception)
            {
                return new WizardTransition(true, BotResults.Back());
            }
        }

        ScreenView view = await initialStep.Renderer(context, state);
        storageState.PayloadJson = JsonSerializer.Serialize(state);

        return new WizardTransition(false, BotResults.ShowView(view));
    }

    public async Task<WizardTransition> ProcessUpdateAsync(UpdateContext context, WizardStorageState storageState)
    {
        EnsureConfigured();

        TState state;
        try
        {
            state = string.IsNullOrWhiteSpace(storageState.PayloadJson)
                ? new TState()
                : JsonSerializer.Deserialize<TState>(storageState.PayloadJson) ?? new TState();
        }
        catch (JsonException)
        {
            return new WizardTransition(true, BotResults.Back());
        }

        if (!_steps!.TryGetValue(storageState.CurrentStepId, out WizardStep<TState>? currentStep))
            throw new InvalidOperationException($"Step {storageState.CurrentStepId} not found");

        StepResult result = await currentStep.Processor(context, state);

        // Удалять текстовое сообщение пользователя нужно при продвижении по шагам.
        // Stay (ошибка валидации) — не удаляем, чтобы пользователь видел что ввёл.
        bool hasIncomingMessage = context.Update.Message is not null;

        if (result is StepResult.GoToResult goTo)
        {
            storageState.StepHistory.Add(storageState.CurrentStepId);
            storageState.CurrentStepId = goTo.StepId;
            storageState.PayloadJson = JsonSerializer.Serialize(state);

            WizardStep<TState> nextStep = _steps[goTo.StepId];

            if (nextStep.OnEnter is not null)
            {
                string stateBeforeEnter = JsonSerializer.Serialize(state);
                try
                {
                    await nextStep.OnEnter(context, state);
                }
                catch (Exception)
                {
                    state = JsonSerializer.Deserialize<TState>(stateBeforeEnter) ?? new TState();
                    ScreenView stayView = await currentStep.Renderer(context, state);
                    return new WizardTransition(false, BotResults.ShowView(stayView));
                }
                storageState.PayloadJson = JsonSerializer.Serialize(state);
            }

            ScreenView view = await nextStep.Renderer(context, state);
            return new WizardTransition(false, BotResults.ShowView(view), ShouldDeleteUserMessage: hasIncomingMessage);
        }

        if (result is StepResult.FinishResult)
        {
            IEndpointResult finalResult = await OnFinishedAsync(context, state);
            return new WizardTransition(true, finalResult, ShouldDeleteUserMessage: hasIncomingMessage);
        }

        if (result is StepResult.StayResult stay)
        {
            storageState.PayloadJson = JsonSerializer.Serialize(state);

            IEndpointResult endpointResult = string.IsNullOrWhiteSpace(stay.Notification)
                ? BotResults.Empty()
                : BotResults.Stay(stay.Notification);

            return new WizardTransition(false, endpointResult, ShouldDeleteUserMessage: false);
        }

        if (result is StepResult.GoBackResult)
            return await GoBackAsync(context, storageState);

        throw new InvalidOperationException("Unknown step result");
    }

    public async Task<WizardTransition> GoBackAsync(UpdateContext context, WizardStorageState storageState)
    {
        EnsureConfigured();

        if (storageState.StepHistory.Count == 0)
        {
            // Первый шаг — выходим из визарда целиком, возвращаясь на предыдущий экран.
            return new WizardTransition(true, BotResults.Back(), WasCancelled: true);
        }

        string previousStepId = storageState.StepHistory[^1];
        storageState.StepHistory.RemoveAt(storageState.StepHistory.Count - 1);
        storageState.CurrentStepId = previousStepId;

        TState state;
        try
        {
            state = string.IsNullOrWhiteSpace(storageState.PayloadJson)
                ? new TState()
                : JsonSerializer.Deserialize<TState>(storageState.PayloadJson) ?? new TState();
        }
        catch (JsonException)
        {
            return new WizardTransition(true, BotResults.Back());
        }

        WizardStep<TState> prevStep = _steps![previousStepId];
        ScreenView view = await prevStep.Renderer(context, state);

        return new WizardTransition(false, BotResults.ShowView(view));
    }

    private void EnsureConfigured()
    {
        if (_steps is not null)
            return;

        var builder = new WizardBuilder<TState>();
        ConfigureSteps(builder);
        _steps = builder.Build(out _initialStepId);
    }
}