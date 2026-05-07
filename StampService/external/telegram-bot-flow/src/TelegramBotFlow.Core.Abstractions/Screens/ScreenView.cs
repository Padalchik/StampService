using System.Reflection;
using System.Text;
using System.Text.Json;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Builder представления экрана: текст, медиа и inline-клавиатура.
///
/// Хранит кнопки как дескрипторы; payload encoding (генерация ShortId, хранение в сессии)
/// выполняется при рендере через <c>BuildKeyboardWithPayloads()</c> в <c>ScreenManager</c>,
/// а не в момент построения представления.
/// </summary>
public sealed class ScreenView
{
    // -- Internal button descriptor types --

    private sealed record CallbackEntry(string Text, string CallbackData);
    private sealed record UrlEntry(string Text, string Url);
    private sealed record PayloadEntry(string Text, string ActionPrefix, string PayloadJson);
    private sealed record RowSeparator;

    private readonly List<object> _buttonList = [];

    // -- Public properties --

    /// <summary>Текст контента экрана.</summary>
    public string Text { get; }

    /// <summary>Медиа-файл экрана, если задан.</summary>
    public InputFile? Media { get; private set; }

    /// <summary>Тип прикреплённого медиа.</summary>
    public ScreenMediaType MediaType { get; private set; } = ScreenMediaType.None;

    /// <summary>
    /// Режим форматирования текста.
    /// По умолчанию <see cref="ParseMode.Html"/>.
    /// Используй <see cref="WithMarkdown"/> или <see cref="WithNoParseMode"/> для изменения.
    /// </summary>
    public ParseMode ParseMode { get; private set; } = ParseMode.Html;

    /// <summary>
    /// When set, the bot will store this action ID in the session as PendingInputActionId
    /// after rendering this screen, enabling the next text message to be routed to the
    /// corresponding input handler.
    /// </summary>
    public string? PendingInputActionId { get; private set; }

    /// <summary>
    /// Factory for building a reply keyboard. When set, the screen will be sent with a
    /// <see cref="Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup"/> built from this factory.
    /// </summary>
    internal Func<ReplyKeyboard, ReplyKeyboard>? ReplyKeyboardFactory { get; private set; }

    /// <summary>
    /// When <see langword="true"/>, the screen will be sent with
    /// <see cref="Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove"/> to hide any active reply keyboard.
    /// </summary>
    public bool ShouldRemoveReplyKeyboard { get; private set; }

    /// <summary>
    /// Computed reply keyboard built from <see cref="ReplyKeyboardFactory"/>, or <see langword="null"/>
    /// if no reply keyboard is configured.
    /// </summary>
    public ReplyKeyboard? ReplyKeyboard =>
        ReplyKeyboardFactory != null ? ReplyKeyboardFactory(new ReplyKeyboard()) : null;

    /// <summary>
    /// <see langword="true"/> если в представлении уже есть кнопка навигации
    /// (<see cref="BackButton"/>, <see cref="CloseButton"/> или <see cref="MenuButton"/>).
    /// Используется <c>ScreenManager</c> для автоматического добавления кнопки,
    /// когда разработчик не добавил её явно.
    /// </summary>
    public bool HasNavigationButton { get; private set; }

    /// <summary>Создаёт представление экрана с текстом.</summary>
    public ScreenView(string text) => Text = text;

    // -- ParseMode --

    /// <summary>
    /// Устанавливает режим форматирования <see cref="Telegram.Bot.Types.Enums.ParseMode.MarkdownV2"/>.
    /// </summary>
    public ScreenView WithMarkdown()
    {
        ParseMode = ParseMode.MarkdownV2;
        return this;
    }

    /// <summary>
    /// Отключает форматирование текста (plain text).
    /// </summary>
    public ScreenView WithNoParseMode()
    {
        ParseMode = ParseMode.None;
        return this;
    }

    // -- Input --

    /// <summary>
    /// Marks this view as awaiting text input. After render, the session's
    /// <c>PendingInputActionId</c> is set to <paramref name="actionId"/>, and the next
    /// non-command message from the user will be routed to the registered input handler.
    /// </summary>
    public ScreenView AwaitInput(string actionId)
    {
        PendingInputActionId = actionId;
        return this;
    }

    /// <summary>
    /// Типизированная версия <see cref="AwaitInput(string)"/>.
    /// Action ID определяется через <see cref="ActionIdResolver"/> (учитывает <see cref="ActionIdAttribute"/>).
    /// </summary>
    public ScreenView AwaitInput<TAction>() where TAction : IBotAction
    {
        PendingInputActionId = ActionIdResolver.GetId<TAction>();
        return this;
    }

    // -- Reply keyboard --

    /// <summary>
    /// Configures a reply keyboard for this screen view. The reply keyboard appears
    /// at the bottom of the chat and supports contact/location requests and custom text buttons.
    /// Mutually exclusive with <see cref="RemoveReplyKeyboard"/>.
    /// </summary>
    public ScreenView WithReplyKeyboard(Func<ReplyKeyboard, ReplyKeyboard> configure)
    {
        ReplyKeyboardFactory = configure;
        ShouldRemoveReplyKeyboard = false;
        return this;
    }

    /// <summary>
    /// Removes any active reply keyboard when this screen is rendered.
    /// Mutually exclusive with <see cref="WithReplyKeyboard"/>.
    /// </summary>
    public ScreenView RemoveReplyKeyboard()
    {
        ReplyKeyboardFactory = null;
        ShouldRemoveReplyKeyboard = true;
        return this;
    }

    // -- Navigation buttons --

    /// <summary>
    /// Adds an inline button that navigates to the specified screen.
    /// Generates callback data: <c>nav:{screenId}</c>.
    /// Screen ID is resolved via <see cref="ScreenIdAttribute"/> if present, otherwise by convention.
    /// </summary>
    public ScreenView NavigateButton<TScreen>(string text) where TScreen : IScreen
    {
        Type screenType = typeof(TScreen);
        var attr = screenType.GetCustomAttribute<ScreenIdAttribute>();
        string screenId = attr?.Id ?? ScreenIdConvention.GetIdFromType(screenType);
        _buttonList.Add(new CallbackEntry(text, $"nav:{screenId}"));
        return this;
    }

    // -- Action buttons --

    /// <summary>
    /// Добавляет callback-кнопку действия с явными данными.
    /// </summary>
    public ScreenView Button(string text, string callbackData)
    {
        _buttonList.Add(new CallbackEntry(text, callbackData));
        return this;
    }

    /// <summary>
    /// Добавляет типизированную кнопку действия.
    /// Callback ID определяется через <see cref="ActionIdResolver"/> (учитывает <see cref="ActionIdAttribute"/>).
    /// </summary>
    public ScreenView Button<TAction>(string text) where TAction : IBotAction
    {
        _buttonList.Add(new CallbackEntry(text, ActionIdResolver.GetId<TAction>()));
        return this;
    }

    /// <summary>
    /// Добавляет типизированную кнопку действия с передачей объекта (Payload).
    /// JSON сериализуется немедленно; выбор между inline (<c>TAction:j:{json}</c>) и
    /// stored (<c>TAction:s:{shortId}</c>) происходит при рендере в <c>ScreenManager</c>.
    /// Action prefix определяется через <see cref="ActionIdResolver"/> (учитывает <see cref="ActionIdAttribute"/>).
    /// </summary>
    public ScreenView Button<TAction, TPayload>(string text, TPayload payload) where TAction : IBotAction
    {
        string json = JsonSerializer.Serialize(payload);
        _buttonList.Add(new PayloadEntry(text, ActionIdResolver.GetId<TAction>(), json));
        return this;
    }

    // -- URL button --

    /// <summary>Добавляет URL-кнопку.</summary>
    public ScreenView UrlButton(string text, string url)
    {
        _buttonList.Add(new UrlEntry(text, url));
        return this;
    }

    // -- Layout --

    /// <summary>Начинает новую строку в клавиатуре.</summary>
    public ScreenView Row()
    {
        _buttonList.Add(new RowSeparator());
        return this;
    }

    // -- System navigation buttons --

    /// <summary>Добавляет кнопку возврата на предыдущий экран из стека.</summary>
    public ScreenView BackButton(string text = "\u2190 Back")
    {
        HasNavigationButton = true;
        Row();
        _buttonList.Add(new CallbackEntry(text, NavCallbacks.BACK));
        return this;
    }

    /// <summary>
    /// Кнопка для action-результатов. Перерисовывает текущий экран из стека
    /// без изменения истории навигации (в отличие от BackButton, которая делает Pop).
    /// </summary>
    public ScreenView CloseButton(string text = "\u2190 Back")
    {
        HasNavigationButton = true;
        Row();
        _buttonList.Add(new CallbackEntry(text, NavCallbacks.CLOSE));
        return this;
    }

    /// <summary>
    /// Кнопка для возврата в главное меню. Очищает всю историю навигации.
    /// </summary>
    public ScreenView MenuButton(string text = "\u2630 Menu")
    {
        HasNavigationButton = true;
        Row();
        _buttonList.Add(new CallbackEntry(text, NavCallbacks.MENU));
        return this;
    }

    // -- Media --

    /// <summary>Добавляет фото по URL.</summary>
    public ScreenView WithPhoto(string url)
    {
        Media = InputFile.FromUri(url);
        MediaType = ScreenMediaType.Photo;
        return this;
    }

    /// <summary>Добавляет фото из Telegram InputFile.</summary>
    public ScreenView WithPhoto(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Photo;
        return this;
    }

    /// <summary>Добавляет видео.</summary>
    public ScreenView WithVideo(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Video;
        return this;
    }

    /// <summary>Добавляет анимацию.</summary>
    public ScreenView WithAnimation(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Animation;
        return this;
    }

    /// <summary>Добавляет документ.</summary>
    public ScreenView WithDocument(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Document;
        return this;
    }

    // -- Framework-internal rendering --

    /// <summary>
    /// Строит клавиатуру и одновременно возвращает payload-словарь для хранения в сессии.
    /// Вызывается только из <c>ScreenManager</c> перед отправкой сообщения.
    ///
    /// Для payload-кнопок: если данные умещаются в 64 байта UTF-8, payload встраивается
    /// в callback_data напрямую (<c>prefix:j:{json}</c>); иначе генерируется ShortId и
    /// payload добавляется в возвращаемый словарь (<c>prefix:s:{shortId}</c>).
    /// </summary>
    internal (InlineKeyboardMarkup? Keyboard, IReadOnlyDictionary<string, string> Payloads)
        BuildKeyboardWithPayloads()
    {
        var kb = new InlineKeyboard();
        var payloads = new Dictionary<string, string>();

        foreach (object item in _buttonList)
        {
            switch (item)
            {
                case CallbackEntry b:
                    kb.Button(b.Text, b.CallbackData);
                    break;

                case UrlEntry u:
                    kb.Url(u.Text, u.Url);
                    break;

                case RowSeparator:
                    kb.Row();
                    break;

                case PayloadEntry p:
                    string inlineData = $"{p.ActionPrefix}:j:{p.PayloadJson}";
                    if (Encoding.UTF8.GetByteCount(inlineData) <= 64)
                    {
                        kb.Button(p.Text, inlineData);
                    }
                    else
                    {
                        string shortId = Guid.NewGuid().ToString("N")[..8];
                        payloads[shortId] = p.PayloadJson;
                        kb.Button(p.Text, $"{p.ActionPrefix}:s:{shortId}");
                    }
                    break;
            }
        }

        return (kb.HasButtons ? kb.Build() : null, payloads);
    }
}

/// <summary>
/// Строковые идентификаторы системных навигационных callback-ов (<c>nav:*</c>).
/// Используй вместо magic strings везде, где нужно сослаться на навигационный callback вручную.
/// </summary>
public static class NavCallbacks
{
    /// <summary>Возврат на предыдущий экран (pop стека навигации).</summary>
    public const string BACK = "nav:back";

    /// <summary>Закрытие action-view без изменения стека (refresh текущего экрана).</summary>
    public const string CLOSE = "nav:close";

    /// <summary>Переход в главное меню с очисткой всей истории навигации.</summary>
    public const string MENU = "nav:menu";
}