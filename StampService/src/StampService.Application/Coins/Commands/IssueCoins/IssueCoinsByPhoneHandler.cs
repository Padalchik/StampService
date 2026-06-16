using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Audit;
using StampService.Application.Brands;
using StampService.Application.CustomerNotifications;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Coins;
using StampService.Domain.Access;
using StampService.Domain.Coins;

namespace StampService.Application.Coins.Commands.IssueCoins;

public class IssueCoinsByPhoneHandler : ICommandHandler<CoinOperationResponse, IssueCoinsByPhoneCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly IBusinessAuditSink _businessAuditSink;
    private readonly ICoinLedgerService _coinLedgerService;
    private readonly ICustomerNotificationService _customerNotificationService;
    private readonly IPhoneAccountService _phoneAccountService;

    public IssueCoinsByPhoneHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        ICoinLedgerService coinLedgerService,
        IPhoneAccountService phoneAccountService,
        ICustomerNotificationService? customerNotificationService = null,
        IBusinessAuditSink? businessAuditSink = null)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _businessAuditSink = businessAuditSink ?? NoopBusinessAuditSink.Instance;
        _coinLedgerService = coinLedgerService;
        _customerNotificationService = customerNotificationService ?? NullCustomerNotificationService.Instance;
        _phoneAccountService = phoneAccountService;
    }

    public async Task<Result<CoinOperationResponse>> Handle(
        IssueCoinsByPhoneCommand command,
        CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
            return await RejectedAsync(command, [BrandErrors.NotFound()], null, null, cancellationToken);

        if (!brand.IsCoinsEnabled)
            return await RejectedAsync(command, [BrandErrors.CoinsDisabled()], null, null, cancellationToken);

        var canIssue = await _brandAccessService.CanAsync(
            command.RequestUserId,
            command.BrandId,
            PermissionCode.StampIssue,
            cancellationToken);

        if (!canIssue)
            return await RejectedAsync(command, [AccessErrors.Denied()], null, null, cancellationToken);

        var comment = string.IsNullOrWhiteSpace(command.Request.Comment)
            ? "Issue coins"
            : command.Request.Comment.Trim();
        var transactionValidation = CoinTransaction.CreateIssue(
            Guid.NewGuid(),
            command.Request.Amount,
            comment,
            command.RequestUserId);
        if (transactionValidation.IsFailed)
            return await RejectedAsync(command, transactionValidation.Errors, null, comment, cancellationToken);

        var customerResult = await _phoneAccountService.GetExistingForBusinessOperationAsync(
            command.Request.PhoneNumber,
            nameof(command.Request.PhoneNumber),
            cancellationToken);
        if (customerResult.IsFailed)
            return await RejectedAsync(command, customerResult.Errors, null, comment, cancellationToken);

        var customer = customerResult.Value;
        var operationResult = await _coinLedgerService.IssueAsync(
            customer.Id,
            command.RequestUserId,
            command.BrandId,
            command.Request.Amount,
            comment,
            cancellationToken);

        if (operationResult.IsFailed)
            return await RejectedAsync(command, operationResult.Errors, customer.Id, comment, cancellationToken);

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

        await _customerNotificationService.NotifyCoinsIssuedAsync(response, cancellationToken);
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.IssueCoins,
                BusinessAuditOperationStatus.Succeeded,
                BrandId: command.BrandId,
                ActorUserId: command.RequestUserId,
                CustomerUserId: customer.Id,
                TargetEntityType: BusinessAuditTargetEntityType.CoinTransaction,
                TargetEntityId: operation.Transaction.Id,
                Amount: command.Request.Amount,
                BalanceBefore: operation.BalanceBefore,
                BalanceAfter: operation.BalanceAfter,
                Comment: comment),
            cancellationToken);

        return Result.Ok(response);
    }

    private async Task<Result<CoinOperationResponse>> RejectedAsync(
        IssueCoinsByPhoneCommand command,
        IReadOnlyCollection<IError> errors,
        Guid? customerUserId,
        string? comment,
        CancellationToken cancellationToken)
    {
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.IssueCoins,
                BusinessAuditOperationStatus.Rejected,
                BrandId: command.BrandId,
                ActorUserId: command.RequestUserId,
                CustomerUserId: customerUserId,
                Amount: command.Request.Amount,
                ReasonCode: BusinessAuditReason.FromErrors(errors),
                Comment: comment),
            cancellationToken);

        return Result.Fail(errors);
    }
}
