namespace StampService.TelegramBot.Features.Admin;

public static class AdminSessionKeys
{
    public const string SelectedBrandId = "admin.selected_brand_id";
    public const string SelectedBrandName = "admin.selected_brand_name";
    public const string SelectedBrandMetricsEnabled = "admin.selected_brand.metrics_enabled";
    public const string SelectedBrandCoinsEnabled = "admin.selected_brand.coins_enabled";
    public const string SelectedOwnerUserId = "admin.selected_owner_user_id";
    public const string SelectedOwnerName = "admin.selected_owner_name";
    public const string SelectedOwnerPhoneNumber = "admin.selected_owner_phone_number";

    public const string CreateBrandName = "admin.create_brand.name";
    public const string CreateOwnerPhoneNumber = "admin.create_brand.owner_phone_number";

    public const string ReassignOwnerPhoneNumber = "admin.reassign_owner.phone_number";

    public const string RewardDigestEditSettingKey = "admin.reward_digest.edit_setting_key";
    public const string RewardDigestEditSettingLabel = "admin.reward_digest.edit_setting_label";

    public const string DemoCustomerCode = "admin.demo.customer_code";
}
