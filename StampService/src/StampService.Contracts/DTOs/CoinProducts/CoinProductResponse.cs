namespace StampService.Contracts.DTOs.CoinProducts;

public record CoinProductResponse(
    Guid Id,
    Guid BrandId,
    string Name,
    int Price,
    bool IsActive,
    DateTime CreatedAt);
