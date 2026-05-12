using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Coins;
using StampService.Domain.Access;
using UserEntity = StampService.Domain.User.User;

namespace StampService.Application.Coins.Commands.IssueCoins;

public class IssueCoinsHandler : ICommandHandler<CoinOperationResponse, IssueCoinsCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly ICoinLedgerService _coinLedgerService;
    private readonly IUserRepository _userRepository;

    public IssueCoinsHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        ICoinLedgerService coinLedgerService,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _coinLedgerService = coinLedgerService;
        _userRepository = userRepository;
    }

    public async Task<Result<CoinOperationResponse>> Handle(
        IssueCoinsCommand command,
        CancellationToken cancellationToken)
    {
        var brandExists = await _brandRepository.ExistsAsync(command.BrandId, cancellationToken);
        if (!brandExists)
            return Result.Fail(BrandErrors.NotFound());

        var canIssue = await _brandAccessService.CanAsync(
            command.RequestUserId,
            command.BrandId,
            PermissionCode.StampIssue,
            cancellationToken);

        if (!canIssue)
            return Result.Fail(AccessErrors.Denied());

        var customerCode = command.CustomerCode.Trim();
        if (!UserEntity.IsValidCustomerCode(customerCode))
            return Result.Fail(UserErrors.CustomerCodeInvalid());

        var customer = await _userRepository.GetByCustomerCodeAsync(customerCode, cancellationToken);
        if (customer is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        var operationResult = await _coinLedgerService.IssueAsync(
            customer.Id,
            command.RequestUserId,
            command.BrandId,
            command.Amount,
            command.Comment,
            cancellationToken);

        if (operationResult.IsFailed)
            return Result.Fail(operationResult.Errors);

        var operation = operationResult.Value;
        return Result.Ok(new CoinOperationResponse(
            operation.Transaction.Id,
            operation.Wallet.Id,
            operation.Wallet.BrandId,
            customer.Id,
            customer.Name,
            customer.CustomerCode,
            operation.Transaction.Type.ToString(),
            operation.Transaction.Amount,
            operation.Wallet.Value,
            operation.Transaction.CreatedAt));
    }
}
