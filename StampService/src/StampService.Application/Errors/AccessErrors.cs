namespace StampService.Application.Errors;

public static class AccessErrors
{
    public static AppError Denied() =>
        AppError.Authorization(
            AppErrorCodes.Access.Denied,
            "Access denied");

    public static AppError AdminRequired() =>
        AppError.Authorization(
            AppErrorCodes.Access.AdminRequired,
            "Admin access required");
}
