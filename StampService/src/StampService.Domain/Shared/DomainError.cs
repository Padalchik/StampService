using FluentResults;

namespace StampService.Domain.Shared;

public enum DomainErrorType
{
    Validation,
    NotFound,
    Failure,
    Conflict
}

public class DomainError : Error
{
    public string Code { get; }
    public DomainErrorType Type { get; }
    public string? InvalidField { get; }

    private DomainError(
        string code,
        string message,
        DomainErrorType type,
        string? invalidField = null) : base(message)
    {
        Code = code;
        Type = type;
        InvalidField = invalidField;
        Metadata["error_code"] = code;
        Metadata["error_type"] = type.ToString();
        if (!string.IsNullOrWhiteSpace(invalidField))
            Metadata["invalid_field"] = invalidField;
    }

    public static DomainError Validation(string code, string message, string? invalidField = null)
    {
        return new DomainError(code, message, DomainErrorType.Validation, invalidField);
    }

    public static DomainError NotFound(string code, string message, string? invalidField = null)
    {
        return new DomainError(code, message, DomainErrorType.NotFound, invalidField);
    }

    public static DomainError Conflict(string code, string message, string? invalidField = null)
    {
        return new DomainError(code, message, DomainErrorType.Conflict, invalidField);
    }

    public static DomainError Failure(string code, string message, string? invalidField = null)
    {
        return new DomainError(code, message, DomainErrorType.Failure, invalidField);
    }
}
