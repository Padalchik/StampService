namespace StampService.Application.Auth;

public record PhoneAuthCodeVerificationResult(
    string PhoneNumber,
    DateTime VerifiedAtUtc);
