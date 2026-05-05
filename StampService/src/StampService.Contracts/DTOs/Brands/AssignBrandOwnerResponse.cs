namespace StampService.Contracts.DTOs.Brands;

public record AssignBrandOwnerResponse(
    Guid MembershipId,
    Guid BrandId,
    Guid UserId,
    string Role,
    DateTime CreatedAt);
