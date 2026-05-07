namespace StampService.Contracts.DTOs.Brands;

public record MyBrandsResponse(
    Guid UserId,
    IReadOnlyCollection<MyBrandResponse> Brands);
