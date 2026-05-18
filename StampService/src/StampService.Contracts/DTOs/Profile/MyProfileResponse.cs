namespace StampService.Contracts.DTOs.Profile;

public record MyProfileResponse(
    Guid UserId,
    string DisplayName,
    string CustomerCode,
    IdentityStatusResponse Telegram,
    IdentityStatusResponse Phone);
