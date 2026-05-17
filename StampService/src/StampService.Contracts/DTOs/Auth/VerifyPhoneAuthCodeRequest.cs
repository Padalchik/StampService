namespace StampService.Contracts.DTOs.Auth;

public record VerifyPhoneAuthCodeRequest(string PhoneNumber, string Code);
