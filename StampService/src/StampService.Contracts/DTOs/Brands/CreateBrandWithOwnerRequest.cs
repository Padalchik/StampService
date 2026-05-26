namespace StampService.Contracts.DTOs.Brands;

public record CreateBrandWithOwnerRequest(
    string BrandName,
    string OwnerPhoneNumber);
