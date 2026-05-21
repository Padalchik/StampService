namespace StampService.Contracts.DTOs.Coins;

public record IssueCoinsByPhoneRequest(
    string PhoneNumber,
    int Amount,
    string? Comment);
