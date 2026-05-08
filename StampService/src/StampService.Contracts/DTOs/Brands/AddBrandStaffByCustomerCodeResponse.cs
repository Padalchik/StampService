namespace StampService.Contracts.DTOs.Brands;

public record AddBrandStaffByCustomerCodeResponse(
    Guid BrandId,
    Guid UserId,
    string UserName,
    string CustomerCode,
    Guid MembershipId,
    DateTime MembershipCreatedAt);
