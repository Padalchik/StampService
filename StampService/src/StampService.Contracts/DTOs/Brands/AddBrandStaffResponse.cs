namespace StampService.Contracts.DTOs.Brands;

public record AddBrandStaffResponse(
    Guid MembershipId,
    Guid BrandId,
    Guid UserId,
    string Role,
    DateTime CreatedAt);
