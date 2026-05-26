namespace StampService.Contracts.DTOs.Demo;

public record CreateUserDemoDataRequest(
    string PhoneNumber,
    Guid BrandId);
