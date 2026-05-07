namespace StampService.Application.Errors;

public static class AccessErrors
{
    public static AppError Denied() =>
        AppError.Authorization(
            "access.denied",
            "Access denied");
}
