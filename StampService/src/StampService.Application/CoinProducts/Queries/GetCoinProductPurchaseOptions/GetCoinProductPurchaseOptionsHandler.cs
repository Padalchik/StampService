using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Coins;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.CoinProducts;
using StampService.Domain.Access;
using DomainRedemptionCode = StampService.Domain.User.RedemptionCode;

namespace StampService.Application.CoinProducts.Queries.GetCoinProductPurchaseOptions;

public class GetCoinProductPurchaseOptionsHandler
    : IQueryHandler<CoinProductPurchaseOptionsResponse, GetCoinProductPurchaseOptionsQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ICoinProductRepository _productRepository;
    private readonly ICoinTransactionRepository _coinTransactionRepository;
    private readonly ICoinWalletRepository _coinWalletRepository;
    private readonly IRedemptionCodeRepository _redemptionCodeRepository;
    private readonly IUserRepository _userRepository;
    private readonly TimeProvider _timeProvider;

    public GetCoinProductPurchaseOptionsHandler(
        IBrandAccessService brandAccessService,
        ICoinProductRepository productRepository,
        ICoinTransactionRepository coinTransactionRepository,
        ICoinWalletRepository coinWalletRepository,
        IRedemptionCodeRepository redemptionCodeRepository,
        IUserRepository userRepository,
        TimeProvider timeProvider)
    {
        _brandAccessService = brandAccessService;
        _productRepository = productRepository;
        _coinTransactionRepository = coinTransactionRepository;
        _coinWalletRepository = coinWalletRepository;
        _redemptionCodeRepository = redemptionCodeRepository;
        _userRepository = userRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CoinProductPurchaseOptionsResponse>> Handle(
        GetCoinProductPurchaseOptionsQuery query,
        CancellationToken cancellationToken)
    {
        var canRedeem = await _brandAccessService.CanAsync(
            query.RequestUserId,
            query.BrandId,
            PermissionCode.StampRedeem,
            cancellationToken);

        if (!canRedeem)
            return Result.Fail(AccessErrors.Denied());

        var code = query.RedemptionCode.Trim();
        if (!DomainRedemptionCode.IsValidCode(code))
            return Result.Fail(UserErrors.RedemptionCodeInvalid());

        var activeCode = await _redemptionCodeRepository.GetActiveByCodeAsync(
            code,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        if (activeCode is null)
            return Result.Fail(UserErrors.RedemptionCodeNotFoundOrExpired());

        var customer = await _userRepository.GetByIdAsync(activeCode.UserId, cancellationToken);
        if (customer is null)
            return Result.Fail(UserErrors.NotFound());

        var wallet = await _coinWalletRepository.GetByUserAndBrandAsync(
            customer.Id,
            query.BrandId,
            cancellationToken);

        var currentBalance = wallet is null
            ? 0
            : await _coinTransactionRepository.CalculateWalletValueAsync(wallet.Id, cancellationToken);

        var products = await _productRepository.GetActiveByBrandAsync(query.BrandId, cancellationToken);
        var options = products
            .OrderBy(product => product.Name)
            .Select(product => new CoinProductPurchaseOptionResponse(
                product.Id,
                product.Name,
                product.Price,
                currentBalance,
                currentBalance >= product.Price))
            .ToArray();

        return Result.Ok(new CoinProductPurchaseOptionsResponse(
            customer.Id,
            customer.Name,
            code,
            options));
    }
}
