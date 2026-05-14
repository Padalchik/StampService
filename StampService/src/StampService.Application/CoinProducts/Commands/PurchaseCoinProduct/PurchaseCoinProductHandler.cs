using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Coins;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Application.Users.Commands.UseRedemptionCode;
using StampService.Contracts.DTOs.Coins;
using StampService.Domain.Access;
using StampService.Domain.Coins;
using DomainRedemptionCode = StampService.Domain.User.RedemptionCode;

namespace StampService.Application.CoinProducts.Commands.PurchaseCoinProduct;

public class PurchaseCoinProductHandler : ICommandHandler<CoinOperationResponse, PurchaseCoinProductCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly ICoinLedgerService _coinLedgerService;
    private readonly ICoinProductRepository _productRepository;
    private readonly ICoinTransactionRepository _coinTransactionRepository;
    private readonly ICoinWalletRepository _coinWalletRepository;
    private readonly IRedemptionCodeRepository _redemptionCodeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICommandHandler<UseRedemptionCodeResponse, UseRedemptionCodeCommand> _useRedemptionCodeHandler;
    private readonly TimeProvider _timeProvider;

    public PurchaseCoinProductHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        ICoinLedgerService coinLedgerService,
        ICoinProductRepository productRepository,
        ICoinTransactionRepository coinTransactionRepository,
        ICoinWalletRepository coinWalletRepository,
        IRedemptionCodeRepository redemptionCodeRepository,
        IUserRepository userRepository,
        ICommandHandler<UseRedemptionCodeResponse, UseRedemptionCodeCommand> useRedemptionCodeHandler,
        TimeProvider timeProvider)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _coinLedgerService = coinLedgerService;
        _productRepository = productRepository;
        _coinTransactionRepository = coinTransactionRepository;
        _coinWalletRepository = coinWalletRepository;
        _redemptionCodeRepository = redemptionCodeRepository;
        _userRepository = userRepository;
        _useRedemptionCodeHandler = useRedemptionCodeHandler;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CoinOperationResponse>> Handle(
        PurchaseCoinProductCommand command,
        CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
            return Result.Fail(BrandErrors.NotFound());

        if (!brand.IsCoinsEnabled)
            return Result.Fail(BrandErrors.CoinsDisabled());

        var canRedeem = await _brandAccessService.CanAsync(
            command.RequestUserId,
            command.BrandId,
            PermissionCode.StampRedeem,
            cancellationToken);

        if (!canRedeem)
            return Result.Fail(AccessErrors.Denied());

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null || product.BrandId != command.BrandId)
            return Result.Fail(CoinProductErrors.NotFound());

        if (!product.IsActive)
            return Result.Fail(CoinProductErrors.IsNotActive());

        var code = command.RedemptionCode.Trim();
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
            command.BrandId,
            cancellationToken);

        if (wallet is null)
            return Result.Fail(CoinErrors.WalletNotFound());

        var currentBalance = await _coinTransactionRepository.CalculateWalletValueAsync(
            wallet.Id,
            cancellationToken);

        if (currentBalance < product.Price)
            return Result.Fail(CoinErrors.InsufficientFunds(currentBalance, product.Price));

        var comment = product.Name;
        var transactionPrecheck = CoinTransaction.CreateRedeem(
            wallet.Id,
            product.Price,
            comment,
            command.RequestUserId);
        if (transactionPrecheck.IsFailed)
            return Result.Fail(transactionPrecheck.Errors);

        var useCodeResult = await _useRedemptionCodeHandler.Handle(
            new UseRedemptionCodeCommand(code),
            cancellationToken);

        if (useCodeResult.IsFailed)
            return Result.Fail(useCodeResult.Errors);

        var operationResult = await _coinLedgerService.RedeemAsync(
            customer.Id,
            command.RequestUserId,
            command.BrandId,
            product.Price,
            comment,
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
