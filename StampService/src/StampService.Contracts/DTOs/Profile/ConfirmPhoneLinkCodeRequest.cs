namespace StampService.Contracts.DTOs.Profile;

public record ConfirmPhoneLinkCodeRequest(string PhoneNumber, string Code, Guid? AuthCodeId = null);
