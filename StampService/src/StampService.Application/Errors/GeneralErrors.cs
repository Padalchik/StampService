namespace StampService.Application.Errors;

public static class GeneralErrors
{
    public static AppError ValueIsInvalid(string field, string? message = null) =>
        AppError.Validation(
            AppErrorCodes.Validation.ValueInvalid,
            message ?? $"{field} is invalid",
            field);

    public static AppError ValueIsRequired(string field, string? message = null) =>
        AppError.Validation(
            AppErrorCodes.Validation.ValueRequired,
            message ?? $"{field} is required",
            field);

    public static AppError Failure(string? message = null) =>
        AppError.Failure(
            AppErrorCodes.General.ServerFailure,
            message ?? "Server error");

    public static AppError NotFound(string? message = null) =>
        AppError.NotFound(
            AppErrorCodes.General.ResourceNotFound,
            message ?? "Resource not found");
}
