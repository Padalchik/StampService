namespace StampService.Application.Errors;

public static class AuthErrors
{
    public static AppError TelegramLoginDataInvalid() =>
        AppError.Authentication(
            AppErrorCodes.Auth.TelegramLoginDataInvalid,
            "Invalid Telegram login data");

    public static AppError UserIdClaimMissing() =>
        AppError.Authentication(
            AppErrorCodes.Auth.UserIdClaimMissing,
            "User id claim is missing");

    public static AppError UserIdClaimInvalid() =>
        AppError.Authentication(
            AppErrorCodes.Auth.UserIdClaimInvalid,
            "User id claim is invalid");
}
