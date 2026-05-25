namespace StampService.Application.Brands;

public record AdminBrandReadModel(
    Guid BrandId,
    string BrandName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    bool IsCoinProductRedemptionEnabled,
    bool IsManualCoinRedemptionEnabled,
    Guid? OwnerUserId,
    string? OwnerName,
    string? OwnerPhoneNumber);
