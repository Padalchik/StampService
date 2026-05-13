namespace StampService.Contracts.DTOs.CoinProducts;

public record CoinProductPurchaseOptionResponse(
    Guid ProductId,
    string ProductName,
    int Price,
    int CurrentBalance,
    bool CanPurchase);
