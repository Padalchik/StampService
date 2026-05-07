namespace StampService.Application.Brands;

public record UserBrandMembershipReadModel(
    Guid BrandId,
    string BrandName,
    string RoleSystemName);
