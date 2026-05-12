using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Users;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.CreateRedemptionCode;

public class CreateRedemptionCodeHandler
    : ICommandHandler<CreateRedemptionCodeResponse, CreateRedemptionCodeCommand>
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(3);

    private readonly IRedemptionCodeGenerator _codeGenerator;
    private readonly IRedemptionCodeRepository _redemptionCodeRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUserRepository _userRepository;

    public CreateRedemptionCodeHandler(
        IRedemptionCodeGenerator codeGenerator,
        IRedemptionCodeRepository redemptionCodeRepository,
        TimeProvider timeProvider,
        IUserRepository userRepository)
    {
        _codeGenerator = codeGenerator;
        _redemptionCodeRepository = redemptionCodeRepository;
        _timeProvider = timeProvider;
        _userRepository = userRepository;
    }

    public async Task<Result<CreateRedemptionCodeResponse>> Handle(
        CreateRedemptionCodeCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var userExists = await _userRepository.ExistsAsync(command.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(UserErrors.NotFound());

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var activeCode = await _redemptionCodeRepository.GetActiveByUserIdAsync(
            command.UserId,
            nowUtc,
            cancellationToken);

        if (activeCode is not null)
        {
            return Result.Ok(new CreateRedemptionCodeResponse(
                activeCode.Code,
                activeCode.ExpiresAtUtc));
        }

        string code;
        try
        {
            code = await _codeGenerator.GenerateAsync(nowUtc, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result.Fail(UserErrors.RedemptionCodePoolExhausted());
        }

        var expiresAtUtc = nowUtc.Add(CodeLifetime);

        var redemptionCodeResult = RedemptionCode.Create(
            command.UserId,
            code,
            expiresAtUtc,
            nowUtc);

        if (redemptionCodeResult.IsFailed)
            return Result.Fail(redemptionCodeResult.Errors);

        _redemptionCodeRepository.Add(redemptionCodeResult.Value);
        await _redemptionCodeRepository.SaveAsync(cancellationToken);

        return Result.Ok(new CreateRedemptionCodeResponse(
            redemptionCodeResult.Value.Code,
            redemptionCodeResult.Value.ExpiresAtUtc));
    }
}
