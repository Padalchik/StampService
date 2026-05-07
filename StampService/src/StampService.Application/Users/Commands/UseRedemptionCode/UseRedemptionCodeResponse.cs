namespace StampService.Application.Users.Commands.UseRedemptionCode;

public record UseRedemptionCodeResponse(
    Guid UserId,
    Guid RedemptionCodeId);
