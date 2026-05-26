namespace StampService.Application.Administration;

public record AdminActor(long? TelegramUserId = null, Guid? UserId = null)
{
    public static AdminActor FromTelegram(long telegramUserId) => new(TelegramUserId: telegramUserId);

    public static AdminActor FromUser(Guid userId) => new(UserId: userId);
}
