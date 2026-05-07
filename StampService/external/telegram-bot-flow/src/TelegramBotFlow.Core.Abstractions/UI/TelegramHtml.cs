namespace TelegramBotFlow.Core.UI;

/// <summary>
/// Утилиты для форматирования HTML-текста в сообщениях Telegram.
/// </summary>
public static class TelegramHtml
{
    /// <summary>
    /// Экранирует специальные HTML-символы для безопасной вставки пользовательского контента
    /// в HTML-форматированное сообщение Telegram.
    /// Заменяет <c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c>.
    /// </summary>
    /// <param name="text">Исходный текст, который может содержать спецсимволы HTML.</param>
    /// <returns>Экранированная строка, безопасная для вставки в HTML-шаблон.</returns>
    public static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>
    /// Оборачивает текст в HTML-тег <c>&lt;b&gt;</c> (жирный).
    /// Автоматически экранирует содержимое.
    /// </summary>
    public static string Bold(string text) => $"<b>{Escape(text)}</b>";

    /// <summary>
    /// Оборачивает текст в HTML-тег <c>&lt;i&gt;</c> (курсив).
    /// Автоматически экранирует содержимое.
    /// </summary>
    public static string Italic(string text) => $"<i>{Escape(text)}</i>";

    /// <summary>
    /// Оборачивает текст в HTML-тег <c>&lt;code&gt;</c> (моноширинный).
    /// Автоматически экранирует содержимое.
    /// </summary>
    public static string Code(string text) => $"<code>{Escape(text)}</code>";
}