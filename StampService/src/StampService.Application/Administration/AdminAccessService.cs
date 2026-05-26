using Microsoft.Extensions.Options;
using StampService.Application.Auth;
using StampService.Application.Users;
using StampService.Domain.User;

namespace StampService.Application.Administration;

public class AdminAccessService : IAdminAccessService
{
    private readonly AdminOptions _options;
    private readonly IUserRepository _userRepository;

    public AdminAccessService(
        IOptions<AdminOptions> options,
        IUserRepository userRepository)
    {
        _options = options.Value;
        _userRepository = userRepository;
    }

    public bool IsAdmin(long telegramUserId)
    {
        return _options.TelegramUserIds.Contains(telegramUserId);
    }

    public async Task<bool> IsAdminAsync(AdminActor actor, CancellationToken cancellationToken)
    {
        if (actor.TelegramUserId is { } telegramUserId && IsAdmin(telegramUserId))
            return true;

        if (actor.UserId is not { } userId)
            return false;

        var configuredPhoneNumbers = _options.PhoneNumbers
            .Select(phoneNumber => PhoneNumberNormalizer.NormalizeForAuth(phoneNumber, nameof(AdminOptions.PhoneNumbers)))
            .Where(result => result.IsSuccess)
            .Select(result => result.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (configuredPhoneNumbers.Count == 0)
            return false;

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return false;

        return user.Identities.Any(identity =>
            identity.DeletedAt is null
            && identity.Type == IdentityType.Phone
            && configuredPhoneNumbers.Contains(identity.Key));
    }
}
