using Microsoft.Extensions.Options;

namespace StampService.Application.Administration;

public class AdminAccessService : IAdminAccessService
{
    private readonly AdminOptions _options;

    public AdminAccessService(IOptions<AdminOptions> options)
    {
        _options = options.Value;
    }

    public bool IsAdmin(long telegramUserId)
    {
        return _options.TelegramUserIds.Contains(telegramUserId);
    }
}
