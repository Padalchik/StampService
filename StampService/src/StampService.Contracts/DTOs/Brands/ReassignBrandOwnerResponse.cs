namespace StampService.Contracts.DTOs.Brands;

public record ReassignBrandOwnerResponse(
    Guid BrandId,
    Guid NewOwnerUserId,
    string NewOwnerName,
    string NewOwnerPhoneNumber,
    Guid MembershipId,
    Guid? RemovedOwnerUserId);
