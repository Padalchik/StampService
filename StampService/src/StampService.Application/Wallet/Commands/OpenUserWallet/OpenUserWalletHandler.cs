using FluentResults;
using Microsoft.Extensions.Logging;
using StampService.Application.Abstractions;
using StampService.Application.CustomerNotifications.Commands.MarkWalletOpened;
using StampService.Application.Users.Commands.CreateRedemptionCode;
using StampService.Application.Wallet.Queries.GetUserWalletOverview;
using StampService.Contracts.DTOs.CustomerNotifications;
using StampService.Contracts.DTOs.Users;
using StampService.Contracts.DTOs.Wallet;

namespace StampService.Application.Wallet.Commands.OpenUserWallet;

public class OpenUserWalletHandler
    : ICommandHandler<UserWalletResponse, OpenUserWalletCommand>
{
    private readonly ICommandHandler<CreateRedemptionCodeResponse, CreateRedemptionCodeCommand> _createCodeHandler;
    private readonly ICommandHandler<MarkWalletOpenedResponse, MarkWalletOpenedCommand> _markWalletOpenedHandler;
    private readonly IQueryHandler<UserWalletOverviewResponse, GetUserWalletOverviewQuery> _overviewHandler;
    private readonly ILogger<OpenUserWalletHandler> _logger;

    public OpenUserWalletHandler(
        ICommandHandler<CreateRedemptionCodeResponse, CreateRedemptionCodeCommand> createCodeHandler,
        ICommandHandler<MarkWalletOpenedResponse, MarkWalletOpenedCommand> markWalletOpenedHandler,
        IQueryHandler<UserWalletOverviewResponse, GetUserWalletOverviewQuery> overviewHandler,
        ILogger<OpenUserWalletHandler> logger)
    {
        _createCodeHandler = createCodeHandler;
        _markWalletOpenedHandler = markWalletOpenedHandler;
        _overviewHandler = overviewHandler;
        _logger = logger;
    }

    public async Task<Result<UserWalletResponse>> Handle(
        OpenUserWalletCommand command,
        CancellationToken cancellationToken)
    {
        var codeResult = await _createCodeHandler.Handle(
            new CreateRedemptionCodeCommand(command.UserId, command.ForceRefreshCode),
            cancellationToken);
        if (codeResult.IsFailed)
            return Result.Fail(codeResult.Errors);

        var overviewResult = await _overviewHandler.Handle(
            new GetUserWalletOverviewQuery(command.UserId),
            cancellationToken);
        if (overviewResult.IsFailed)
            return Result.Fail(overviewResult.Errors);

        await MarkWalletOpenedBestEffortAsync(command.UserId, cancellationToken);

        return Result.Ok(new UserWalletResponse(
            overviewResult.Value.UserId,
            new UserWalletRedemptionCodeResponse(
                codeResult.Value.Code,
                codeResult.Value.ExpiresAtUtc),
            overviewResult.Value.Brands));
    }

    private async Task MarkWalletOpenedBestEffortAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var markResult = await _markWalletOpenedHandler.Handle(
                new MarkWalletOpenedCommand(userId),
                cancellationToken);
            if (markResult.IsFailed)
            {
                _logger.LogWarning(
                    "Wallet opened marker failed. UserId={UserId} Errors={Errors}",
                    userId,
                    string.Join("; ", markResult.Errors.Select(error => error.Message)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Wallet opened marker failed with exception. UserId={UserId}",
                userId);
        }
    }
}
