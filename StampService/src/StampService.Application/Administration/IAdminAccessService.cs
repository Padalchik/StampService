namespace StampService.Application.Administration;

public interface IAdminAccessService
{
    bool IsAdmin(long telegramUserId);

    Task<bool> IsAdminAsync(AdminActor actor, CancellationToken cancellationToken);
}
