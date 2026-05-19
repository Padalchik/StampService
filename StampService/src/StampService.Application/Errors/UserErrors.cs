using StampService.Domain.User;

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

    public static AppError IdentityAlreadyLinked() =>
        AppError.Conflict(
            AppErrorCodes.User.IdentityAlreadyLinked,
            "Identity is already linked to this user");

    public static AppError IdentityLinkedToAnotherUser() =>
        AppError.Conflict(
            AppErrorCodes.User.IdentityLinkedToAnotherUser,
            "Identity is already linked to another user");

    public static AppError IdentityMergeNotAllowed() =>
        AppError.Conflict(
            AppErrorCodes.User.IdentityMergeNotAllowed,
            "Account merge is not allowed for these users");

    public static AppError IdentityMergeSourceHasMultipleIdentities() =>
        AppError.Conflict(
            AppErrorCodes.User.IdentityMergeSourceHasMultipleIdentities,
            "Account merge is not allowed because the source account has more than one login method");

    public static AppError IdentityMergeTargetHasBrandMembership() =>
        AppError.Conflict(
            AppErrorCodes.User.IdentityMergeTargetHasBrandMembership,
            "Account merge is not allowed because both accounts have access to the same brand");

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
            $"Redemption code must contain exactly {RedemptionCode.CodeLength} digits",
            "redemptionCode");

    public static AppError RedemptionCodeNotFoundOrExpired() =>
        AppError.NotFound(
            AppErrorCodes.RedemptionCode.NotFoundOrExpired,
            "Redemption code not found or expired");

    public static AppError RedemptionCodePoolExhausted() =>
        AppError.Conflict(
            AppErrorCodes.RedemptionCode.PoolExhausted,
            "No redemption codes are available at the moment");
}
