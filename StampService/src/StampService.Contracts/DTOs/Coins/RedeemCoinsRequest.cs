namespace StampService.Contracts.DTOs.Coins;

public record RedeemCoinsRequest(
    string RedemptionCode,
    int Amount,
    string Comment);
