namespace StampService.Application.Errors;

public static class AuthErrors
{
    public static AppError PhoneInvalid(string? invalidField = null) =>
        AppError.Validation(
            AppErrorCodes.Auth.PhoneInvalid,
            "Phone number is invalid",
            invalidField);

    public static AppError PhoneCodeInvalid() =>
        AppError.Authentication(
            AppErrorCodes.Auth.PhoneCodeInvalid,
            "Phone auth code is invalid or expired");

    public static AppError PhoneCodeSendFailed(string message) =>
        AppError.Failure(
            AppErrorCodes.Auth.PhoneCodeSendFailed,
            message);

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
