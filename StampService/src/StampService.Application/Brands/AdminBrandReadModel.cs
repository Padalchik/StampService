namespace StampService.Application.Brands;

public record AdminBrandReadModel(
    Guid BrandId,
    string BrandName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    Guid? OwnerUserId,
    string? OwnerName,
    string? OwnerCustomerCode);
