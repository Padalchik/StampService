using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ReplyMarkup = Telegram.Bot.Types.ReplyMarkups.ReplyMarkup;

namespace TelegramBotFlow.Core.Context;

/// <summary>
/// Абстракция отправки и редактирования сообщений пользователю в рамках update-а.
/// </summary>
public interface IUpdateResponder
{
    /// <summary>
    /// Отправляет новое сообщение в чат пользователя.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <param name="text">Текст сообщения.</param>
    /// <param name="replyMarkup">Клавиатура или другое reply-markup.</param>
    /// <param name="parseMode">Режим парсинга текста.</param>
    /// <returns>Отправленное сообщение Telegram.</returns>
    Task<Message> ReplyAsync(UpdateContext context, string text, ReplyMarkup? replyMarkup = null,
        ParseMode parseMode = ParseMode.Html);

    /// <summary>
    /// Редактирует текущее сообщение, связанное с контекстом.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <param name="text">Новый текст сообщения.</param>
    /// <param name="replyMarkup">Inline-клавиатура сообщения.</param>
    /// <param name="parseMode">Режим парсинга текста.</param>
    Task EditMessageAsync(UpdateContext context, string text, InlineKeyboardMarkup? replyMarkup = null,
        ParseMode parseMode = ParseMode.Html);

    /// <summary>
    /// Редактирует сообщение по его идентификатору.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <param name="messageId">Идентификатор редактируемого сообщения.</param>
    /// <param name="text">Новый текст сообщения.</param>
    /// <param name="replyMarkup">Inline-клавиатура сообщения.</param>
    /// <param name="parseMode">Режим парсинга текста.</param>
    Task EditMessageAsync(UpdateContext context, int messageId, string text, InlineKeyboardMarkup? replyMarkup = null,
        ParseMode parseMode = ParseMode.Html);

    /// <summary>
    /// Удаляет сообщение по идентификатору.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <param name="messageId">Идентификатор удаляемого сообщения.</param>
    Task DeleteMessageAsync(UpdateContext context, int messageId);

    /// <summary>
    /// Удаляет текущее сообщение из контекста, если его ID доступен.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    Task DeleteMessageAsync(UpdateContext context);

    /// <summary>
    /// Отвечает на callback-запрос Telegram-кнопки.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <param name="text">Текст уведомления callback (опционально).</param>
    /// <param name="showAlert">Показывать модальное уведомление вместо toast.</param>
    Task AnswerCallbackAsync(UpdateContext context, string? text = null, bool showAlert = false);

    /// <summary>
    /// Копирует сообщение из другого чата в чат пользователя.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <param name="fromChatId">Идентификатор чата-источника.</param>
    /// <param name="messageId">Идентификатор копируемого сообщения.</param>
    Task CopyMessageAsync(UpdateContext context, long fromChatId, int messageId);

    /// <summary>
    /// Удаляет текущий якорь сессии, копирует сообщение с заданной клавиатурой и сохраняет его
    /// как новый якорь навигации. Использовать вместо <see cref="CopyMessageAsync"/> когда
    /// скопированное сообщение должно стать основным экраном пользователя (roadmap, контент-посты).
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <param name="fromChatId">Идентификатор чата-источника.</param>
    /// <param name="messageId">Идентификатор копируемого сообщения.</param>
    /// <param name="replyMarkup">Inline-клавиатура, прикрепляемая к скопированному сообщению.</param>
    Task ReplaceAnchorWithCopyAsync(UpdateContext context, long fromChatId, int messageId,
        InlineKeyboardMarkup? replyMarkup = null);
}