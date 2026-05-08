namespace StampService.Application.Errors;

public static class UserErrors
{
    public static AppError NotFound() =>
        AppError.NotFound(
            AppErrorCodes.User.NotFound,
            "User not found");

    public static AppError RecipientNotFound() =>
        AppError.NotFound(
            AppErrorCodes.Recipient.NotFound,
            "Recipient not found");

    public static AppError IdIsEmpty() =>
        AppError.Validation(
            AppErrorCodes.User.IdEmpty,
            "User id cannot be empty",
            "userId");

    public static AppError TelegramUserIdMustBePositive() =>
        AppError.Validation(
            AppErrorCodes.Telegram.UserIdInvalid,
            "Telegram user id must be positive",
            "telegramUserId");

    public static AppError CustomerCodeInvalid() =>
        AppError.Validation(
            AppErrorCodes.CustomerCode.Invalid,
            "Customer code must contain exactly 4 digits",
            "customerCode");

    public static AppError RedemptionCodeInvalid() =>
        AppError.Validation(
            AppErrorCodes.RedemptionCode.Invalid,
            "Redemption code must contain exactly 6 digits",
            "redemptionCode");

    public static AppError RedemptionCodeNotFoundOrExpired() =>
        AppError.NotFound(
            AppErrorCodes.RedemptionCode.NotFoundOrExpired,
            "Redemption code not found or expired");
}
