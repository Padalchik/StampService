using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.EnsureTelegramUser;

public class EnsureTelegramUserHandler
    : ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneAccountService _phoneAccountService;

    public EnsureTelegramUserHandler(
        IUserRepository userRepository,
        IPhoneAccountService phoneAccountService)
    {
        _userRepository = userRepository;
        _phoneAccountService = phoneAccountService;
    }

    public async Task<Result<EnsureTelegramUserResponse>> Handle(
        EnsureTelegramUserCommand command,
        CancellationToken cancellationToken)
    {
        if (command.TelegramUserId <= 0)
            return Result.Fail(UserErrors.TelegramUserIdMustBePositive());

        var providerKey = command.TelegramUserId.ToString();
        var user = await _userRepository.GetByIdentityAsync(
            IdentityType.Telegram,
            providerKey,
            cancellationToken);

        if (user is not null)
        {
            if (!_phoneAccountService.HasActivePhoneIdentity(user))
                return Result.Fail(UserErrors.TelegramIdentityNotLinked());

            return Result.Ok(new EnsureTelegramUserResponse(
                user.Id,
                Created: false,
                user.Name));
        }

        return Result.Fail(UserErrors.TelegramIdentityNotLinked());
    }
}
