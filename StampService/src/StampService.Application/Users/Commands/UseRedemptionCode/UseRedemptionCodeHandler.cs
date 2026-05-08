using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Errors;
using StampService.Domain.User;

namespace StampService.Application.Users.Commands.UseRedemptionCode;

public class UseRedemptionCodeHandler
    : ICommandHandler<UseRedemptionCodeResponse, UseRedemptionCodeCommand>
{
    private readonly IRedemptionCodeRepository _redemptionCodeRepository;
    private readonly TimeProvider _timeProvider;

    public UseRedemptionCodeHandler(
        IRedemptionCodeRepository redemptionCodeRepository,
        TimeProvider timeProvider)
    {
        _redemptionCodeRepository = redemptionCodeRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<UseRedemptionCodeResponse>> Handle(
        UseRedemptionCodeCommand command,
        CancellationToken cancellationToken)
    {
        var code = command.RedemptionCode?.Trim() ?? string.Empty;
        if (!RedemptionCode.IsValidCode(code))
            return Result.Fail(UserErrors.RedemptionCodeInvalid());

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var redemptionCode = await _redemptionCodeRepository.GetActiveByCodeAsync(
            code,
            nowUtc,
            cancellationToken);

        if (redemptionCode is null)
            return Result.Fail(UserErrors.RedemptionCodeNotFoundOrExpired());

        var useResult = redemptionCode.Use(nowUtc);
        if (useResult.IsFailed)
            return Result.Fail(useResult.Errors);

        // Persistence is delegated to the caller's unit of work.
        // For redeem this lets the code usage and ledger transaction be saved atomically.
        return Result.Ok(new UseRedemptionCodeResponse(
            redemptionCode.UserId,
            redemptionCode.Id));
    }
}
