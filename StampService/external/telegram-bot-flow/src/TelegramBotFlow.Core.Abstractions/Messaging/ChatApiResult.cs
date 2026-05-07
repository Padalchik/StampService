namespace TelegramBotFlow.Core.Messaging;

/// <summary>
/// Result-тип для <see cref="IChatAdministrationApi"/>. Никаких exception'ов на бизнес-фейлы —
/// caller сам решает что делать. <see cref="IsSuccess"/> + <see cref="Value"/> для happy-path,
/// <see cref="ErrorCode"/> + <see cref="ErrorMessage"/> для фейла.
///
/// Использует свой минимальный Result (а не CSharpFunctionalExtensions) чтобы Abstractions
/// проект не тащил dependency на 3rd-party Result-библиотеку.
/// </summary>
public sealed class ChatApiResult<T>
{
    private ChatApiResult(bool isSuccess, T? value, ChatApiErrorCode errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public ChatApiErrorCode ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static ChatApiResult<T> Success(T value) =>
        new(true, value, ChatApiErrorCode.None, null);

    public static ChatApiResult<T> Failure(ChatApiErrorCode code, string? message = null) =>
        new(false, default, code, message);
}

public enum ChatApiErrorCode
{
    None = 0,
    /// <summary>Чат недоступен: бот не в чате, чат удалён, либо API timeout.</summary>
    ChatNotReachable = 1,
    /// <summary>Bot API rate-limit (429). Caller может ретраить.</summary>
    RateLimited = 2,
    /// <summary>Прочая ошибка Telegram Bot API (например 400 PARAMETER_INVALID).</summary>
    Other = 3
}
