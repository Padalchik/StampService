namespace StampService.Contracts.DTOs.Brands;

public record BrandStaffResponse(
    Guid UserId,
    string UserName,
    string CustomerCode,
    DateTime MembershipCreatedAt);
