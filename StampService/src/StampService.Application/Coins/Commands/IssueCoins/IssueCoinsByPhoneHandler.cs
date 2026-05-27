using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
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
    private readonly ICoinLedgerService _coinLedgerService;
    private readonly ICustomerNotificationService _customerNotificationService;
    private readonly IPhoneAccountService _phoneAccountService;

    public IssueCoinsByPhoneHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        ICoinLedgerService coinLedgerService,
        IPhoneAccountService phoneAccountService,
        ICustomerNotificationService? customerNotificationService = null)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
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
            return Result.Fail(BrandErrors.NotFound());

        if (!brand.IsCoinsEnabled)
            return Result.Fail(BrandErrors.CoinsDisabled());

        var canIssue = await _brandAccessService.CanAsync(
            command.RequestUserId,
            command.BrandId,
            PermissionCode.StampIssue,
            cancellationToken);

        if (!canIssue)
            return Result.Fail(AccessErrors.Denied());

        var comment = string.IsNullOrWhiteSpace(command.Request.Comment)
            ? "Issue coins"
            : command.Request.Comment.Trim();
        var transactionValidation = CoinTransaction.CreateIssue(
            Guid.NewGuid(),
            command.Request.Amount,
            comment,
            command.RequestUserId);
        if (transactionValidation.IsFailed)
            return Result.Fail(transactionValidation.Errors);

        var customerResult = await _phoneAccountService.GetOrCreateForBusinessOperationAsync(
            command.Request.PhoneNumber,
            nameof(command.Request.PhoneNumber),
            cancellationToken);
        if (customerResult.IsFailed)
            return Result.Fail(customerResult.Errors);

        var customer = customerResult.Value;
        var operationResult = await _coinLedgerService.IssueAsync(
            customer.Id,
            command.RequestUserId,
            command.BrandId,
            command.Request.Amount,
            comment,
            cancellationToken);

        if (operationResult.IsFailed)
            return Result.Fail(operationResult.Errors);

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

        return Result.Ok(response);
    }
}
