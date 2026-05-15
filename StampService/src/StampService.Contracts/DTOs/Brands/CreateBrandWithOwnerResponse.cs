namespace StampService.Contracts.DTOs.Brands;

public record CreateBrandWithOwnerResponse(
    Guid BrandId,
    string BrandName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    bool IsCoinProductRedemptionEnabled,
    bool IsManualCoinRedemptionEnabled,
    Guid OwnerUserId,
    string OwnerName,
    string OwnerCustomerCode,
    Guid MembershipId,
    DateTime CreatedAt);
