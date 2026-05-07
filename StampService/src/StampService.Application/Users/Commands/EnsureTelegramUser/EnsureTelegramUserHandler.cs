using System.Text.Json;
using FluentResults;
using StampService.Application.Abstractions;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.EnsureTelegramUser;

public class EnsureTelegramUserHandler
    : ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand>
{
    private readonly IUserRepository _userRepository;

    public EnsureTelegramUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<EnsureTelegramUserResponse>> Handle(
        EnsureTelegramUserCommand command,
        CancellationToken cancellationToken)
    {
        if (command.TelegramUserId <= 0)
            return Result.Fail("Telegram user id must be positive");

        var providerKey = command.TelegramUserId.ToString();
        var user = await _userRepository.GetByIdentityAsync(
            IdentityType.Telegram,
            providerKey,
            cancellationToken);

        if (user is not null)
        {
            return Result.Ok(new EnsureTelegramUserResponse(
                user.Id,
                Created: false,
                user.Name));
        }

        var displayName = GetDisplayName(command);
        var userResult = User.Create(displayName);
        if (userResult.IsFailed)
            return Result.Fail(userResult.Errors);

        user = userResult.Value;

        var metadata = JsonSerializer.Serialize(new
        {
            command.TelegramUserId,
            command.FirstName,
            command.LastName,
            command.Username
        });

        var identityResult = user.AddIdentity(
            IdentityType.Telegram,
            providerKey,
            metadata);

        if (identityResult.IsFailed)
            return Result.Fail(identityResult.Errors);

        _userRepository.Add(user);
        await _userRepository.SaveAsync(cancellationToken);

        return Result.Ok(new EnsureTelegramUserResponse(
            user.Id,
            Created: true,
            user.Name));
    }

    private static string GetDisplayName(EnsureTelegramUserCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.Username))
            return command.Username.Trim();

        var name = $"{command.FirstName} {command.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name)
            ? command.TelegramUserId.ToString()
            : name;
    }
}
