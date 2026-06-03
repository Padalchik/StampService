namespace StampService.Application.Audit;

public static class BusinessAuditOperationType
{
    public const string IssueCoins = "IssueCoins";
    public const string RedeemCoins = "RedeemCoins";
    public const string IssueMetric = "IssueMetric";
    public const string RedeemMetric = "RedeemMetric";
    public const string PurchaseCoinProduct = "PurchaseCoinProduct";
    public const string AddStaff = "AddStaff";
    public const string UpdateRewardSettings = "UpdateRewardSettings";
}
