namespace StampService.Contracts.DTOs.Brands;

public record UpdateBrandResponse(
    Guid BrandId,
    string BrandName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    bool IsCoinProductRedemptionEnabled,
    bool IsManualCoinRedemptionEnabled,
    DateTime? UpdatedAt);
