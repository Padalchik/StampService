namespace StampService.Contracts.DTOs.Brands;

public record AdminBrandResponse(
    Guid BrandId,
    string BrandName,
    Guid? OwnerUserId,
    string? OwnerName,
    string? OwnerCustomerCode);
