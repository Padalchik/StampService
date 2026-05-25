namespace StampService.Contracts.DTOs.Brands;

public record RemoveBrandStaffResponse(
    Guid BrandId,
    Guid UserId,
    string UserName,
    string? PhoneNumber);
