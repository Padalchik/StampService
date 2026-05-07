namespace TelegramBotFlow.Core.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при попытке получить из сессии устаревший или удалённый payload кнопки.
/// </summary>
public sealed class PayloadExpiredException : Exception
{
    public PayloadExpiredException()
        : base("Button data expired. Please refresh the menu.")
    {
    }
}