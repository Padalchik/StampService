namespace StampService.Application.Administration;

public interface IAdminAccessService
{
    bool IsAdmin(long telegramUserId);
}
