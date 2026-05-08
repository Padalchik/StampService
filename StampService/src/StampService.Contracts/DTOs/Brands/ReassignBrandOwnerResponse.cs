namespace StampService.Contracts.DTOs.Brands;

public record ReassignBrandOwnerResponse(
    Guid BrandId,
    Guid NewOwnerUserId,
    string NewOwnerName,
    string NewOwnerCustomerCode,
    Guid MembershipId,
    Guid? RemovedOwnerUserId);
