namespace StampService.TelegramBot.Features.Coins;

public static class CoinSessionKeys
{
    public const string Mode = "coins.mode";
    public const string CustomerCode = "coins.customer_code";
    public const string RedemptionCode = "coins.redemption_code";
    public const string Amount = "coins.amount";
    public const string Comment = "coins.comment";

    public const string ModeIssue = "issue";
    public const string ModeRedeem = "redeem";
    public const string ModeBalance = "balance";
}
