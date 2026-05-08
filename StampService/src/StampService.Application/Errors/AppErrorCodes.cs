namespace StampService.Application.Errors;

public static class AppErrorCodes
{
    public static class Access
    {
        public const string Denied = "access.denied";
        public const string AdminRequired = "access.admin_required";
    }

    public static class Auth
    {
        public const string TelegramLoginDataInvalid = "auth.telegram_login_data_invalid";
        public const string UserIdClaimMissing = "auth.user_id_claim_missing";
        public const string UserIdClaimInvalid = "auth.user_id_claim_invalid";
    }

    public static class Brand
    {
        public const string IdEmpty = "brand.id_empty";
        public const string NotFound = "brand.not_found";
        public const string OwnerAlreadyExists = "brand.owner_already_exists";
    }

    public static class BrandMembership
    {
        public const string NotFound = "brand_membership.not_found";
        public const string CannotChangeOwnerRole = "brand_membership.cannot_change_owner_role";
    }

    public static class CustomerCode
    {
        public const string Invalid = "customer_code.invalid";
    }

    public static class General
    {
        public const string ServerFailure = "server.failure";
        public const string ResourceNotFound = "resource.not_found";
    }

    public static class Paging
    {
        public const string SkipNegative = "paging.skip_negative";
        public const string TakeOutOfRange = "paging.take_out_of_range";
    }

    public static class Metric
    {
        public const string NotFound = "metric.not_found";
        public const string Inactive = "metric.inactive";
        public const string CodeAlreadyExistsForBrand = "metric.code_already_exists_for_brand";
    }

    public static class MetricBalance
    {
        public const string NotFound = "metric_balance.not_found";
        public const string InsufficientFunds = "metric_balance.insufficient_funds";
    }

    public static class RedemptionCode
    {
        public const string Invalid = "redemption_code.invalid";
        public const string NotFoundOrExpired = "redemption_code.not_found_or_expired";
        public const string AlreadyUsed = "redemption_code.already_used";
    }

    public static class Recipient
    {
        public const string NotFound = "recipient.not_found";
    }

    public static class Role
    {
        public const string OwnerNotFound = "role.owner_not_found";
        public const string StaffNotFound = "role.staff_not_found";
    }

    public static class Telegram
    {
        public const string UserIdInvalid = "telegram.user_id_invalid";
    }

    public static class User
    {
        public const string IdEmpty = "user.id_empty";
        public const string NotFound = "user.not_found";
    }

    public static class Validation
    {
        public const string ValueInvalid = "validation.value_invalid";
        public const string ValueRequired = "validation.value_required";
    }
}
