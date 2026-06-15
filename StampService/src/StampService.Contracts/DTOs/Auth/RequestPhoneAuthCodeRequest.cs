namespace StampService.Contracts.DTOs.Auth;

public record RequestPhoneAuthCodeRequest(string PhoneNumber, bool SendSms = false);
