namespace StampService.Application.Auth;

public record PhoneAuthCodeRequestResult(
    string PhoneNumber,
    DateTime ExpiresAtUtc,
    Guid AuthCodeId);
