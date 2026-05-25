namespace StampService.Contracts.DTOs.Brands;

public record AdminBrandResponse(
    Guid BrandId,
    string BrandName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    bool IsCoinProductRedemptionEnabled,
    bool IsManualCoinRedemptionEnabled,
    Guid? OwnerUserId,
    string? OwnerName,
    string? OwnerPhoneNumber);
