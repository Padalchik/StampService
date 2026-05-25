namespace StampService.Contracts.DTOs.Profile;

public record MyProfileResponse(
    Guid UserId,
    string DisplayName,
    IdentityStatusResponse Telegram,
    IdentityStatusResponse Phone);
