namespace StampService.TelegramBot.Features.Staff.Actions;

public record OpenStaffDetailsPayload(
    Guid UserId,
    string UserName,
    string CustomerCode);
