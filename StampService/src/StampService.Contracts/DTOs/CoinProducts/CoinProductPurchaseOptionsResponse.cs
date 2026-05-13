namespace StampService.Contracts.DTOs.CoinProducts;

public record CoinProductPurchaseOptionsResponse(
    Guid CustomerUserId,
    string CustomerName,
    string RedemptionCode,
    IReadOnlyCollection<CoinProductPurchaseOptionResponse> Products);
