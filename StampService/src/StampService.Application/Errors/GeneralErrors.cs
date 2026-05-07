namespace StampService.Application.Errors;

public static class GeneralErrors
{
    public static AppError ValueIsInvalid(string field, string? message = null) =>
        AppError.Validation(
            "validation.value_invalid",
            message ?? $"{field} is invalid",
            field);

    public static AppError ValueIsRequired(string field, string? message = null) =>
        AppError.Validation(
            "validation.value_required",
            message ?? $"{field} is required",
            field);

    public static AppError Failure(string? message = null) =>
        AppError.Failure(
            "server.failure",
            message ?? "Server error");

    public static AppError NotFound(string? message = null) =>
        AppError.NotFound(
            "resource.not_found",
            message ?? "Resource not found");
}
