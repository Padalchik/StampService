namespace StampService.Contracts.DTOs.Brands;

public record CreateBrandWithOwnerResponse(
    Guid BrandId,
    string BrandName,
    Guid OwnerUserId,
    string OwnerName,
    string OwnerCustomerCode,
    Guid MembershipId,
    DateTime CreatedAt);
