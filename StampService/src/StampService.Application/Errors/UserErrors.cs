namespace StampService.Application.Errors;

public static class UserErrors
{
    public static AppError NotFound() =>
        AppError.NotFound(
            "user.not_found",
            "User not found");

    public static AppError RecipientNotFound() =>
        AppError.NotFound(
            "recipient.not_found",
            "Recipient not found");

    public static AppError IdIsEmpty() =>
        AppError.Validation(
            "user.id_empty",
            "User id cannot be empty",
            "userId");

    public static AppError TelegramUserIdMustBePositive() =>
        AppError.Validation(
            "telegram.user_id_invalid",
            "Telegram user id must be positive",
            "telegramUserId");

    public static AppError CustomerCodeInvalid() =>
        AppError.Validation(
            "customer_code.invalid",
            "Customer code must contain exactly 4 digits",
            "customerCode");

    public static AppError RedemptionCodeInvalid() =>
        AppError.Validation(
            "redemption_code.invalid",
            "Redemption code must contain exactly 6 digits",
            "redemptionCode");

    public static AppError RedemptionCodeNotFoundOrExpired() =>
        AppError.NotFound(
            "redemption_code.not_found_or_expired",
            "Redemption code not found or expired");
}
