namespace StampService.Contracts.DTOs.Profile;

public record RequestPhoneLinkCodeResponse(DateTime ExpiresAt, Guid AuthCodeId);
