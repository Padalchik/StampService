using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Convenience-расширения для <see cref="WizardBuilder{TState}"/>.
/// Покрывают два самых распространённых паттерна шагов:
/// текстовый ввод (TextStep) и выбор из inline-кнопок (ButtonStep).
/// </summary>
public static class WizardBuilderExtensions
{
    private const string SKIP_CALLBACK = "wizard:skip";

    // ── TextStep ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Добавляет шаг текстового ввода с чистым (pure) обработчиком.
    /// Автоматически выполняет trim и empty-guard; вызывает <paramref name="onInput"/>
    /// с уже обрезанным непустым текстом.
    /// <para>
    /// Предпочтительный вариант: обработчик получает только состояние и текст,
    /// без доступа к <see cref="UpdateContext"/>. Это делает шаг легко тестируемым
    /// без моков и DI-контейнера.
    /// </para>
    /// </summary>
    /// <param name="builder">Строитель визарда.</param>
    /// <param name="id">ID шага.</param>
    /// <param name="prompt">Текст вопроса (статичный).</param>
    /// <param name="onInput">
    /// Чистый обработчик ввода: <c>(state, text) → StepResult</c>.
    /// При <paramref name="isOptional"/> = <see langword="true"/> может получить <see cref="string.Empty"/> (пропуск).
    /// </param>
    /// <param name="emptyMessage">Уведомление при пустом вводе (только если <paramref name="isOptional"/> = <see langword="false"/>).</param>
    /// <param name="isOptional">
    /// Если <see langword="true"/>, пустой ввод или нажатие кнопки «Пропустить» передаёт
    /// <see cref="string.Empty"/> в <paramref name="onInput"/> вместо показа ошибки.
    /// </param>
    /// <param name="skipButtonText">Текст кнопки пропуска (видна только при <paramref name="isOptional"/> = <see langword="true"/>).</param>
    public static WizardBuilder<TState> TextStep<TState>(
        this WizardBuilder<TState> builder,
        string id,
        string prompt,
        Func<TState, string, StepResult> onInput,
        string emptyMessage = "Введите значение.",
        bool isOptional = false,
        string skipButtonText = "Пропустить →")
        where TState : class, new() =>
        builder.TextStep(id, prompt, (_, state, text) => onInput(state, text), emptyMessage, isOptional, skipButtonText);

    /// <summary>
    /// Добавляет шаг текстового ввода с чистым (pure) динамическим текстом вопроса и чистым обработчиком.
    /// </summary>
    /// <param name="builder">Строитель визарда.</param>
    /// <param name="id">ID шага.</param>
    /// <param name="prompt">Чистая функция формирования текста вопроса: <c>state → string</c>.</param>
    /// <param name="onInput">Чистый обработчик ввода: <c>(state, text) → StepResult</c>.</param>
    /// <param name="emptyMessage">Уведомление при пустом вводе.</param>
    /// <param name="isOptional">Если <see langword="true"/>, пустой ввод не является ошибкой.</param>
    /// <param name="skipButtonText">Текст кнопки пропуска.</param>
    public static WizardBuilder<TState> TextStep<TState>(
        this WizardBuilder<TState> builder,
        string id,
        Func<TState, string> prompt,
        Func<TState, string, StepResult> onInput,
        string emptyMessage = "Введите значение.",
        bool isOptional = false,
        string skipButtonText = "Пропустить →")
        where TState : class, new() =>
        builder.TextStep(id, (_, state) => prompt(state), (_, state, text) => onInput(state, text), emptyMessage, isOptional, skipButtonText);

    /// <summary>
    /// Добавляет шаг текстового ввода.
    /// Автоматически выполняет trim и empty-guard; вызывает <paramref name="onInput"/>
    /// с уже обрезанным непустым текстом.
    /// <para>
    /// Используй эту перегрузку только когда шагу нужен доступ к <see cref="UpdateContext"/>
    /// (например, для обращения к DI-сервисам). Для большинства шагов
    /// используй <c>TextStep(id, prompt, (state, text) => ...)</c>.
    /// </para>
    /// </summary>
    /// <param name="builder">Строитель визарда.</param>
    /// <param name="id">ID шага.</param>
    /// <param name="prompt">Текст вопроса (статичный).</param>
    /// <param name="onInput">
    /// Синхронная логика обработки ввода. Получает готовый trimmed текст.
    /// При <paramref name="isOptional"/> = <see langword="true"/> может получить <see cref="string.Empty"/> (пропуск).
    /// </param>
    /// <param name="emptyMessage">Уведомление при пустом вводе (только если <paramref name="isOptional"/> = <see langword="false"/>).</param>
    /// <param name="isOptional">
    /// Если <see langword="true"/>, пустой ввод или нажатие кнопки «Пропустить» передаёт
    /// <see cref="string.Empty"/> в <paramref name="onInput"/> вместо показа ошибки.
    /// </param>
    /// <param name="skipButtonText">Текст кнопки пропуска (видна только при <paramref name="isOptional"/> = <see langword="true"/>).</param>
    public static WizardBuilder<TState> TextStep<TState>(
        this WizardBuilder<TState> builder,
        string id,
        string prompt,
        Func<UpdateContext, TState, string, StepResult> onInput,
        string emptyMessage = "Введите значение.",
        bool isOptional = false,
        string skipButtonText = "Пропустить →")
        where TState : class, new() => builder.Step(
        id: id,
        renderer: (_, _) =>
        {
            ScreenView view = new(prompt);
            if (isOptional)
                view = view.Row().Button(skipButtonText, SKIP_CALLBACK);
            return view;
        },
        processor: (ctx, state) =>
        {
            bool isSkip = isOptional && ctx.CallbackData == SKIP_CALLBACK;
            string text = ctx.MessageText?.Trim() ?? string.Empty;

            if (!isSkip && string.IsNullOrEmpty(text))
            {
                return isOptional
                    ? Task.FromResult(onInput(ctx, state, string.Empty))
                    : Task.FromResult<StepResult>(StepResult.Stay(emptyMessage));
            }

            return Task.FromResult(onInput(ctx, state, isSkip ? string.Empty : text));
        });

    /// <summary>
    /// Добавляет шаг текстового ввода с динамическим текстом вопроса (зависит от состояния).
    /// <para>
    /// Используй эту перегрузку только когда шагу нужен доступ к <see cref="UpdateContext"/>.
    /// Для большинства шагов используй <c>TextStep(id, state => prompt, (state, text) => ...)</c>.
    /// </para>
    /// </summary>
    /// <inheritdoc cref="TextStep{TState}(WizardBuilder{TState},string,string,Func{UpdateContext,TState,string,StepResult},string,bool,string)"/>
    public static WizardBuilder<TState> TextStep<TState>(
        this WizardBuilder<TState> builder,
        string id,
        Func<UpdateContext, TState, string> prompt,
        Func<UpdateContext, TState, string, StepResult> onInput,
        string emptyMessage = "Введите значение.",
        bool isOptional = false,
        string skipButtonText = "Пропустить →")
        where TState : class, new()
    {
        return builder.Step(
            id: id,
            renderer: (ctx, state) =>
            {
                ScreenView view = new(prompt(ctx, state));
                if (isOptional)
                    view = view.Row().Button(skipButtonText, SKIP_CALLBACK);
                return view;
            },
            processor: (ctx, state) =>
            {
                bool isSkip = isOptional && ctx.CallbackData == SKIP_CALLBACK;
                string text = ctx.MessageText?.Trim() ?? string.Empty;

                if (!isSkip && string.IsNullOrEmpty(text))
                {
                    return isOptional
                        ? Task.FromResult(onInput(ctx, state, string.Empty))
                        : Task.FromResult<StepResult>(StepResult.Stay(emptyMessage));
                }

                return Task.FromResult<StepResult>(onInput(ctx, state, isSkip ? string.Empty : text));
            });
    }

    // ── ButtonStep ────────────────────────────────────────────────────────────

    /// <summary>
    /// Добавляет шаг выбора из inline-кнопок с чистым (pure) обработчиком.
    /// Кнопки задаются один раз: renderer строит клавиатуру из них,
    /// processor автоматически валидирует callback data.
    /// <para>
    /// Предпочтительный вариант: обработчик получает только состояние и выбранное значение,
    /// без доступа к <see cref="UpdateContext"/>.
    /// </para>
    /// </summary>
    /// <param name="builder">Строитель визарда.</param>
    /// <param name="id">ID шага.</param>
    /// <param name="prompt">Текст вопроса (статичный).</param>
    /// <param name="buttons">Список кнопок в виде <c>(Label, CallbackValue)</c>.</param>
    /// <param name="onSelected">
    /// Чистый обработчик выбора: <c>(state, value) → StepResult</c>.
    /// Получает <c>value</c> нажатой кнопки — всегда из <paramref name="buttons"/>.
    /// </param>
    /// <param name="columns">
    /// Количество кнопок в строке. <c>0</c> = все в одну строку (по умолчанию).
    /// При <c>columns &gt; 0</c> клавиатура разбивается на строки по N кнопок.
    /// </param>
    public static WizardBuilder<TState> ButtonStep<TState>(
        this WizardBuilder<TState> builder,
        string id,
        string prompt,
        IReadOnlyList<(string Label, string Value)> buttons,
        Func<TState, string, StepResult> onSelected,
        int columns = 0)
        where TState : class, new() =>
        builder.ButtonStep(id, prompt, buttons, (_, state, value) => onSelected(state, value), columns);

    /// <summary>
    /// Добавляет шаг выбора из inline-кнопок с чистым (pure) динамическим текстом вопроса и чистым обработчиком.
    /// </summary>
    /// <param name="builder">Строитель визарда.</param>
    /// <param name="id">ID шага.</param>
    /// <param name="prompt">Чистая функция формирования текста вопроса: <c>state → string</c>.</param>
    /// <param name="buttons">Список кнопок в виде <c>(Label, CallbackValue)</c>.</param>
    /// <param name="onSelected">Чистый обработчик выбора: <c>(state, value) → StepResult</c>.</param>
    /// <param name="columns">Количество кнопок в строке. <c>0</c> = все в одну строку.</param>
    public static WizardBuilder<TState> ButtonStep<TState>(
        this WizardBuilder<TState> builder,
        string id,
        Func<TState, string> prompt,
        IReadOnlyList<(string Label, string Value)> buttons,
        Func<TState, string, StepResult> onSelected,
        int columns = 0)
        where TState : class, new() =>
        builder.ButtonStep(id, (_, state) => prompt(state), buttons, (_, state, value) => onSelected(state, value), columns);

    /// <summary>
    /// Добавляет шаг выбора из inline-кнопок со статичным текстом вопроса.
    /// Кнопки задаются один раз: renderer строит клавиатуру из них,
    /// processor автоматически валидирует <see cref="UpdateContext.CallbackData"/>.
    /// <para>
    /// Используй эту перегрузку только когда шагу нужен доступ к <see cref="UpdateContext"/>.
    /// Для большинства шагов используй <c>ButtonStep(id, prompt, buttons, (state, value) => ...)</c>.
    /// </para>
    /// </summary>
    /// <param name="builder">Строитель визарда.</param>
    /// <param name="id">ID шага.</param>
    /// <param name="prompt">Текст вопроса (статичный).</param>
    /// <param name="buttons">Список кнопок в виде <c>(Label, CallbackValue)</c>.</param>
    /// <param name="onSelected">
    /// Синхронная логика после выбора. Получает <c>value</c> нажатой кнопки — всегда из <paramref name="buttons"/>.
    /// </param>
    /// <param name="columns">
    /// Количество кнопок в строке. <c>0</c> = все в одну строку (по умолчанию).
    /// При <c>columns &gt; 0</c> клавиатура разбивается на строки по N кнопок.
    /// </param>
    public static WizardBuilder<TState> ButtonStep<TState>(
        this WizardBuilder<TState> builder,
        string id,
        string prompt,
        IReadOnlyList<(string Label, string Value)> buttons,
        Func<UpdateContext, TState, string, StepResult> onSelected,
        int columns = 0)
        where TState : class, new()
    {
        return ButtonStep(builder, id, (_, _) => prompt, buttons, onSelected, columns);
    }

    /// <summary>
    /// Добавляет шаг выбора из inline-кнопок с динамическим текстом вопроса
    /// (зависит от текущего состояния визарда).
    /// <para>
    /// Используй эту перегрузку только когда шагу нужен доступ к <see cref="UpdateContext"/>.
    /// Для большинства шагов используй <c>ButtonStep(id, state => prompt, buttons, (state, value) => ...)</c>.
    /// </para>
    /// </summary>
    /// <param name="builder">Строитель визарда.</param>
    /// <param name="id">ID шага.</param>
    /// <param name="prompt">Функция формирования текста вопроса на основе контекста и состояния.</param>
    /// <param name="buttons">Список кнопок в виде <c>(Label, CallbackValue)</c>.</param>
    /// <param name="onSelected">
    /// Синхронная логика после выбора. Получает <c>value</c> нажатой кнопки — всегда из <paramref name="buttons"/>.
    /// </param>
    /// <param name="columns">
    /// Количество кнопок в строке. <c>0</c> = все в одну строку (по умолчанию).
    /// При <c>columns &gt; 0</c> клавиатура разбивается на строки по N кнопок.
    /// </param>
    public static WizardBuilder<TState> ButtonStep<TState>(
        this WizardBuilder<TState> builder,
        string id,
        Func<UpdateContext, TState, string> prompt,
        IReadOnlyList<(string Label, string Value)> buttons,
        Func<UpdateContext, TState, string, StepResult> onSelected,
        int columns = 0)
        where TState : class, new()
    {
        if (buttons.Count == 0)
            throw new ArgumentException("ButtonStep requires at least one button.", nameof(buttons));

        HashSet<string> validValues = buttons.Select(b => b.Value).ToHashSet(StringComparer.Ordinal);

        return builder.Step(
            id: id,
            renderer: (ctx, state) =>
            {
                ScreenView view = new(prompt(ctx, state));

                for (int i = 0; i < buttons.Count; i++)
                {
                    if (columns > 0 && i > 0 && i % columns == 0)
                        view = view.Row();

                    view = view.Button(buttons[i].Label, buttons[i].Value);
                }

                return view;
            },
            processor: (ctx, state) =>
            {
                if (ctx.CallbackData is null || !validValues.Contains(ctx.CallbackData))
                    return Task.FromResult<StepResult>(StepResult.Stay());

                return Task.FromResult(onSelected(ctx, state, ctx.CallbackData));
            });
    }
}