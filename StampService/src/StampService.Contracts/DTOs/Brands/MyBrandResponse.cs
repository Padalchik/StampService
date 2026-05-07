namespace StampService.Contracts.DTOs.Brands;

public record MyBrandResponse(
    Guid BrandId,
    string BrandName,
    string RoleSystemName);
