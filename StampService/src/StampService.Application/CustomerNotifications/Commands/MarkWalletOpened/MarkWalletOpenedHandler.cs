using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.CustomerNotifications;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.CustomerNotifications;
using StampService.Domain.User;

namespace StampService.Application.CustomerNotifications.Commands.MarkWalletOpened;

public class MarkWalletOpenedHandler
    : ICommandHandler<MarkWalletOpenedResponse, MarkWalletOpenedCommand>
{
    private readonly ICustomerDigestStateRepository _stateRepository;
    private readonly TimeProvider _timeProvider;

    public MarkWalletOpenedHandler(
        ICustomerDigestStateRepository stateRepository,
        TimeProvider timeProvider)
    {
        _stateRepository = stateRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MarkWalletOpenedResponse>> Handle(
        MarkWalletOpenedCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var state = await _stateRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        if (state is null)
        {
            var createResult = CustomerDigestState.Create(command.UserId);
            if (createResult.IsFailed)
                return Result.Fail(createResult.Errors);

            state = createResult.Value;
            _stateRepository.Add(state);
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        state.MarkWalletOpened(nowUtc);
        await _stateRepository.SaveAsync(cancellationToken);

        return Result.Ok(new MarkWalletOpenedResponse(command.UserId, nowUtc));
    }
}
