using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Users;

namespace TelegramBotFlow.Core.Context;

/// <summary>
/// Контекст обработки одного Telegram update с вычисленными полями и runtime-состоянием.
/// </summary>
public sealed class UpdateContext
{
    /// <summary>
    /// Исходный Telegram update.
    /// </summary>
    public Update Update { get; }

    /// <summary>
    /// Scope-провайдер сервисов для текущей обработки update-а.
    /// </summary>
    internal IServiceProvider RequestServices { get; }

    /// <summary>
    /// Токен отмены обработки.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Сессия пользователя, если подключено session middleware.
    /// </summary>
    public UserSession? Session { get; set; }

    /// <summary>
    /// Флаг административного доступа, вычисляемый политикой доступа.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Current bot user, set by UserTrackingMiddleware. Null if middleware not registered.
    /// </summary>
    public IBotUser? User { get; internal set; }

    /// <summary>
    /// Name of the matched handler, set by UpdateRouter at dispatch time. For structured logging.
    /// </summary>
    public string? HandlerName { get; internal set; }

    /// <summary>
    /// Идентификатор чата из update-а.
    /// </summary>
    public long ChatId { get; }

    /// <summary>
    /// Идентификатор пользователя из update-а.
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// Идентификатор сообщения, если доступен.
    /// </summary>
    public int? MessageId { get; }

    /// <summary>
    /// Данные callback-кнопки, если update является callback.
    /// </summary>
    public string? CallbackData { get; }

    /// <summary>
    /// Текст входящего сообщения, если присутствует.
    /// </summary>
    public string? MessageText { get; }

    /// <summary>
    /// Аргумент команды после пробела, например в <c>/start value</c>.
    /// </summary>
    public string? CommandArgument { get; }

    /// <summary>
    /// Массив фотографий из входящего сообщения (разные размеры одной фотографии).
    /// </summary>
    public PhotoSize[]? Photos { get; }

    /// <summary>
    /// Документ из входящего сообщения.
    /// </summary>
    public Document? Document { get; }

    /// <summary>
    /// Контакт из входящего сообщения.
    /// </summary>
    public Contact? Contact { get; }

    /// <summary>
    /// Локация из входящего сообщения.
    /// </summary>
    public Location? Location { get; }

    /// <summary>
    /// Голосовое сообщение из входящего сообщения.
    /// </summary>
    public Voice? Voice { get; }

    /// <summary>
    /// Видеосообщение (кружок) из входящего сообщения.
    /// </summary>
    public VideoNote? VideoNote { get; }

    /// <summary>
    /// Видео из входящего сообщения.
    /// </summary>
    public Video? Video { get; }

    /// <summary>
    /// Указывает, содержит ли сообщение медиа-контент (фото, документ, голосовое, видео, видеосообщение).
    /// </summary>
    public bool HasMedia => Photos != null || Document != null
                         || Voice != null || Video != null || VideoNote != null;

    /// <summary>
    /// Тип входящего update.
    /// </summary>
    public UpdateType UpdateType { get; }

    /// <summary>
    /// Текущий экран из пользовательской сессии.
    /// </summary>
    public string? Screen => Session?.Navigation.CurrentScreen;

    /// <summary>
    /// Инициализирует контекст обработки update-а.
    /// </summary>
    /// <param name="update">Исходный Telegram update.</param>
    /// <param name="requestServices">Scope-провайдер сервисов текущего запроса.</param>
    /// <param name="cancellationToken">Токен отмены обработки.</param>
    public UpdateContext(
        Update update,
        IServiceProvider requestServices,
        CancellationToken cancellationToken = default)
    {
        Update = update;
        RequestServices = requestServices;
        CancellationToken = cancellationToken;

        UpdateType = update.Type;
        ChatId = ExtractChatId(update);
        UserId = ExtractUserId(update);
        MessageId = ExtractMessageId(update);
        CallbackData = update.CallbackQuery?.Data;
        MessageText = update.Message?.Text;
        CommandArgument = ExtractCommandArgument(MessageText);
        Photos = update.Message?.Photo;
        Document = update.Message?.Document;
        Contact = update.Message?.Contact;
        Location = update.Message?.Location;
        Voice = update.Message?.Voice;
        VideoNote = update.Message?.VideoNote;
        Video = update.Message?.Video;
    }

    // -- Extractors --

    private static long ExtractChatId(Update update) =>
        update.Type switch
        {
            UpdateType.Message => update.Message!.Chat.Id,
            UpdateType.CallbackQuery => update.CallbackQuery!.Message!.Chat.Id,
            UpdateType.EditedMessage => update.EditedMessage!.Chat.Id,
            UpdateType.ChannelPost => update.ChannelPost!.Chat.Id,
            _ => 0
        };

    private static long ExtractUserId(Update update) =>
        update.Type switch
        {
            UpdateType.Message => update.Message!.From!.Id,
            UpdateType.CallbackQuery => update.CallbackQuery!.From.Id,
            UpdateType.EditedMessage => update.EditedMessage!.From!.Id,
            _ => 0
        };

    private static int? ExtractMessageId(Update update) =>
        update.Type switch
        {
            UpdateType.Message => update.Message!.Id,
            UpdateType.CallbackQuery => update.CallbackQuery!.Message?.Id,
            UpdateType.EditedMessage => update.EditedMessage!.Id,
            UpdateType.ChannelPost => update.ChannelPost!.Id,
            _ => null
        };

    private static string? ExtractCommandArgument(string? messageText)
    {
        if (messageText is null || !messageText.StartsWith('/'))
            return null;

        int spaceIndex = messageText.IndexOf(' ');
        if (spaceIndex < 0)
            return null;

        string argument = messageText[(spaceIndex + 1)..].Trim();
        return argument.Length > 0 ? argument : null;
    }
}