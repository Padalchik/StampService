namespace StampService.Application.Errors;

public static class AccessErrors
{
    public static AppError Denied() =>
        AppError.Authorization(
            AppErrorCodes.Access.Denied,
            "Access denied");
}
