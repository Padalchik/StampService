namespace StampService.Application.Errors;

public static class AuthErrors
{
    public static AppError TelegramLoginDataInvalid() =>
        AppError.Authentication(
            "auth.telegram_login_data_invalid",
            "Invalid Telegram login data");

    public static AppError UserIdClaimMissing() =>
        AppError.Authentication(
            "auth.user_id_claim_missing",
            "User id claim is missing");

    public static AppError UserIdClaimInvalid() =>
        AppError.Authentication(
            "auth.user_id_claim_invalid",
            "User id claim is invalid");
}
