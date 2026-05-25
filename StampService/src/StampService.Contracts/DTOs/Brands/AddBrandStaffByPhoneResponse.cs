namespace StampService.Contracts.DTOs.Brands;

public record AddBrandStaffByPhoneResponse(
    Guid BrandId,
    Guid UserId,
    string UserName,
    string PhoneNumber,
    Guid MembershipId,
    DateTime MembershipCreatedAt);
