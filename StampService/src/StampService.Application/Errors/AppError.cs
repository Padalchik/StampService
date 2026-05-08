using FluentResults;

namespace StampService.Application.Errors;

public class AppError : Error
{
    public string Code { get; }
    public AppErrorType Type { get; }
    public string? InvalidField { get; }

    private AppError(
        string code,
        string message,
        AppErrorType type,
        string? invalidField = null)
        : base(message)
    {
        Code = code;
        Type = type;
        InvalidField = invalidField;
        Metadata["error_code"] = code;
        Metadata["error_type"] = type.ToString();
        if (!string.IsNullOrWhiteSpace(invalidField))
            Metadata["invalid_field"] = invalidField;
    }

    public AppError WithMetadataValue(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }

    public static AppError Validation(string code, string message, string? invalidField = null) =>
        new(code, message, AppErrorType.Validation, invalidField);

    public static AppError NotFound(string code, string message) =>
        new(code, message, AppErrorType.NotFound);

    public static AppError Failure(string code, string message) =>
        new(code, message, AppErrorType.Failure);

    public static AppError Conflict(string code, string message) =>
        new(code, message, AppErrorType.Conflict);

    public static AppError Authentication(string code, string message) =>
        new(code, message, AppErrorType.Authentication);

    public static AppError Authorization(string code, string message) =>
        new(code, message, AppErrorType.Authorization);
}
