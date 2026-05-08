namespace StampService.Application.Brands;

public record BrandStaffReadModel(
    Guid UserId,
    string UserName,
    string CustomerCode,
    DateTime MembershipCreatedAt);
