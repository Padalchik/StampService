using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Coins;
using StampService.Domain.Access;
using UserEntity = StampService.Domain.User.User;

namespace StampService.Application.Coins.Queries.GetCoinHistory;

public class GetCoinHistoryHandler : IQueryHandler<CoinTransactionsResponse, GetCoinHistoryQuery>
{
    private const int MaxTake = 100;

    private readonly IBrandAccessService _brandAccessService;
    private readonly ICoinWalletRepository _coinWalletRepository;
    private readonly ICoinTransactionRepository _coinTransactionRepository;
    private readonly IUserRepository _userRepository;

    public GetCoinHistoryHandler(
        IBrandAccessService brandAccessService,
        ICoinWalletRepository coinWalletRepository,
        ICoinTransactionRepository coinTransactionRepository,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _coinWalletRepository = coinWalletRepository;
        _coinTransactionRepository = coinTransactionRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<CoinTransactionsResponse>> Handle(
        GetCoinHistoryQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Skip < 0)
            return Result.Fail(PagingErrors.SkipCannotBeNegative());

        if (query.Take <= 0 || query.Take > MaxTake)
            return Result.Fail(PagingErrors.TakeOutOfRange(MaxTake));

        var canViewBalance = await _brandAccessService.CanAsync(
            query.RequestUserId,
            query.BrandId,
            PermissionCode.BalanceView,
            cancellationToken);

        if (!canViewBalance)
            return Result.Fail(AccessErrors.Denied());

        var customerCode = query.CustomerCode.Trim();
        if (!UserEntity.IsValidCustomerCode(customerCode))
            return Result.Fail(UserErrors.CustomerCodeInvalid());

        var customer = await _userRepository.GetByCustomerCodeAsync(customerCode, cancellationToken);
        if (customer is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        var wallet = await _coinWalletRepository.GetByUserAndBrandAsync(
            customer.Id,
            query.BrandId,
            cancellationToken);

        if (wallet is null)
        {
            return Result.Ok(new CoinTransactionsResponse(
                query.BrandId,
                customer.Id,
                query.Skip,
                query.Take,
                []));
        }

        var transactions = await _coinTransactionRepository.GetHistoryByWalletAsync(
            wallet.Id,
            query.Skip,
            query.Take,
            cancellationToken);

        var items = transactions
            .Select(transaction => new CoinTransactionResponse(
                transaction.Id,
                wallet.Id,
                wallet.BrandId,
                wallet.UserId,
                transaction.Type.ToString(),
                transaction.Amount,
                transaction.Comment,
                transaction.CreatedAt))
            .ToArray();

        return Result.Ok(new CoinTransactionsResponse(
            query.BrandId,
            customer.Id,
            query.Skip,
            query.Take,
            items));
    }
}
