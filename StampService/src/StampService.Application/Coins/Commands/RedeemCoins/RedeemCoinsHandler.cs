using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Audit;
using StampService.Application.Brands;
using StampService.Application.CustomerNotifications;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Application.Users.Commands.UseRedemptionCode;
using StampService.Contracts.DTOs.Coins;
using StampService.Domain.Access;
using StampService.Domain.Coins;
using DomainRedemptionCode = StampService.Domain.User.RedemptionCode;

namespace StampService.Application.Coins.Commands.RedeemCoins;

public class RedeemCoinsHandler : ICommandHandler<CoinOperationResponse, RedeemCoinsCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly IBusinessAuditSink _businessAuditSink;
    private readonly ICoinLedgerService _coinLedgerService;
    private readonly ICustomerNotificationService _customerNotificationService;
    private readonly ICoinTransactionRepository _coinTransactionRepository;
    private readonly ICoinWalletRepository _coinWalletRepository;
    private readonly IRedemptionCodeRepository _redemptionCodeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICommandHandler<UseRedemptionCodeResponse, UseRedemptionCodeCommand> _useRedemptionCodeHandler;
    private readonly TimeProvider _timeProvider;

    public RedeemCoinsHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        ICoinLedgerService coinLedgerService,
        ICoinTransactionRepository coinTransactionRepository,
        ICoinWalletRepository coinWalletRepository,
        IRedemptionCodeRepository redemptionCodeRepository,
        IUserRepository userRepository,
        ICommandHandler<UseRedemptionCodeResponse, UseRedemptionCodeCommand> useRedemptionCodeHandler,
        TimeProvider timeProvider,
        ICustomerNotificationService? customerNotificationService = null,
        IBusinessAuditSink? businessAuditSink = null)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _businessAuditSink = businessAuditSink ?? NoopBusinessAuditSink.Instance;
        _coinLedgerService = coinLedgerService;
        _customerNotificationService = customerNotificationService ?? NullCustomerNotificationService.Instance;
        _coinTransactionRepository = coinTransactionRepository;
        _coinWalletRepository = coinWalletRepository;
        _redemptionCodeRepository = redemptionCodeRepository;
        _userRepository = userRepository;
        _useRedemptionCodeHandler = useRedemptionCodeHandler;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CoinOperationResponse>> Handle(
        RedeemCoinsCommand command,
        CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
            return await RejectedAsync(command, [BrandErrors.NotFound()], null, null, null, cancellationToken);

        if (!brand.IsCoinsEnabled)
            return await RejectedAsync(command, [BrandErrors.CoinsDisabled()], null, null, null, cancellationToken);

        if (!brand.IsManualCoinRedemptionEnabled)
            return await RejectedAsync(command, [BrandErrors.ManualCoinRedemptionDisabled()], null, null, null, cancellationToken);

        var canRedeem = await _brandAccessService.CanAsync(
            command.RequestUserId,
            command.BrandId,
            PermissionCode.StampRedeem,
            cancellationToken);

        if (!canRedeem)
            return await RejectedAsync(command, [AccessErrors.Denied()], null, null, null, cancellationToken);

        var code = command.RedemptionCode.Trim();
        if (!DomainRedemptionCode.IsValidCode(code))
            return await RejectedAsync(command, [UserErrors.RedemptionCodeInvalid()], null, null, null, cancellationToken);

        var activeCode = await _redemptionCodeRepository.GetActiveByCodeAsync(
            code,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        if (activeCode is null)
            return await RejectedAsync(command, [UserErrors.RedemptionCodeNotFoundOrExpired()], null, null, null, cancellationToken);

        var customer = await _userRepository.GetByIdAsync(activeCode.UserId, cancellationToken);
        if (customer is null)
            return await RejectedAsync(command, [UserErrors.NotFound()], activeCode.UserId, null, null, cancellationToken);

        var wallet = await _coinWalletRepository.GetByUserAndBrandAsync(
            customer.Id,
            command.BrandId,
            cancellationToken);

        if (wallet is null)
            return await RejectedAsync(command, [CoinErrors.WalletNotFound()], customer.Id, null, null, cancellationToken);

        var currentBalance = await _coinTransactionRepository.CalculateWalletValueAsync(
            wallet.Id,
            cancellationToken);

        if (currentBalance < command.Amount)
            return await RejectedAsync(
                command,
                [CoinErrors.InsufficientFunds(currentBalance, command.Amount)],
                customer.Id,
                currentBalance,
                null,
                cancellationToken);

        var comment = string.IsNullOrWhiteSpace(command.Comment)
            ? "Manual coin redemption"
            : command.Comment.Trim();
        var transactionPrecheck = CoinTransaction.CreateRedeem(
            wallet.Id,
            command.Amount,
            comment,
            command.RequestUserId);
        if (transactionPrecheck.IsFailed)
            return await RejectedAsync(command, transactionPrecheck.Errors, customer.Id, currentBalance, comment, cancellationToken);

        var useCodeResult = await _useRedemptionCodeHandler.Handle(
            new UseRedemptionCodeCommand(code),
            cancellationToken);

        if (useCodeResult.IsFailed)
            return await RejectedAsync(command, useCodeResult.Errors, customer.Id, currentBalance, comment, cancellationToken);

        var operationResult = await _coinLedgerService.RedeemAsync(
            customer.Id,
            command.RequestUserId,
            command.BrandId,
            command.Amount,
            comment,
            cancellationToken);

        if (operationResult.IsFailed)
            return await RejectedAsync(command, operationResult.Errors, customer.Id, currentBalance, comment, cancellationToken);

        var operation = operationResult.Value;
        var response = new CoinOperationResponse(
            operation.Transaction.Id,
            operation.Wallet.Id,
            operation.Wallet.BrandId,
            customer.Id,
            customer.Name,
            operation.Transaction.Type.ToString(),
            operation.Transaction.Amount,
            operation.Wallet.Value,
            operation.Transaction.CreatedAt);

        await _customerNotificationService.NotifyCoinsRedeemedAsync(response, comment, cancellationToken);
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.RedeemCoins,
                BusinessAuditOperationStatus.Succeeded,
                BrandId: command.BrandId,
                ActorUserId: command.RequestUserId,
                CustomerUserId: customer.Id,
                TargetEntityType: BusinessAuditTargetEntityType.CoinTransaction,
                TargetEntityId: operation.Transaction.Id,
                Amount: command.Amount,
                BalanceBefore: operation.BalanceBefore,
                BalanceAfter: operation.BalanceAfter,
                Comment: comment),
            cancellationToken);

        return Result.Ok(response);
    }

    private async Task<Result<CoinOperationResponse>> RejectedAsync(
        RedeemCoinsCommand command,
        IReadOnlyCollection<IError> errors,
        Guid? customerUserId,
        int? balanceBefore,
        string? comment,
        CancellationToken cancellationToken)
    {
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.RedeemCoins,
                BusinessAuditOperationStatus.Rejected,
                BrandId: command.BrandId,
                ActorUserId: command.RequestUserId,
                CustomerUserId: customerUserId,
                Amount: command.Amount,
                BalanceBefore: balanceBefore,
                ReasonCode: BusinessAuditReason.FromErrors(errors),
                Comment: comment),
            cancellationToken);

        return Result.Fail(errors);
    }
}
