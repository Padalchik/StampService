namespace StampService.Application.Brands;

public record AdminBrandReadModel(
    Guid BrandId,
    string BrandName,
    Guid? OwnerUserId,
    string? OwnerName,
    string? OwnerCustomerCode);
