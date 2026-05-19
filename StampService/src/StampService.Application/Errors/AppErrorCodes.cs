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
        public const string PhoneCodeInvalid = "auth.phone_code_invalid";
        public const string PhoneCodeSendFailed = "auth.phone_code_send_failed";
        public const string PhoneInvalid = "auth.phone_invalid";
        public const string TelegramCodeInvalid = "auth.telegram_code_invalid";
        public const string TelegramCodeSendFailed = "auth.telegram_code_send_failed";
        public const string TelegramLoginDataInvalid = "auth.telegram_login_data_invalid";
        public const string UserIdClaimMissing = "auth.user_id_claim_missing";
        public const string UserIdClaimInvalid = "auth.user_id_claim_invalid";
    }

    public static class Brand
    {
        public const string IdEmpty = "brand.id_empty";
        public const string CoinsDisabled = "brand.coins_disabled";
        public const string CoinProductRedemptionDisabled = "brand.coin_product_redemption_disabled";
        public const string ManualCoinRedemptionDisabled = "brand.manual_coin_redemption_disabled";
        public const string MetricsDisabled = "brand.metrics_disabled";
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

    public static class Coin
    {
        public const string WalletNotFound = "coin.wallet_not_found";
        public const string InsufficientFunds = "coin.insufficient_funds";
    }

    public static class CoinProduct
    {
        public const string NotFound = "coin_product.not_found";
        public const string Inactive = "coin_product.inactive";
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
        public const string PoolExhausted = "redemption_code.pool_exhausted";
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
        public const string IdentityAlreadyLinked = "user.identity_already_linked";
        public const string IdentityLinkedToAnotherUser = "user.identity_linked_to_another_user";
        public const string IdentityMergeNotAllowed = "user.identity_merge_not_allowed";
        public const string IdentityMergeSourceHasMultipleIdentities = "user.identity_merge_source_has_multiple_identities";
        public const string IdentityMergeTargetHasBrandMembership = "user.identity_merge_target_has_brand_membership";
        public const string NotFound = "user.not_found";
    }

    public static class Validation
    {
        public const string ValueInvalid = "validation.value_invalid";
        public const string ValueRequired = "validation.value_required";
    }
}
