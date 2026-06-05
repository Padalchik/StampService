using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Audit;
using StampService.Application.Brands;
using StampService.Application.Coins;
using StampService.Application.CustomerNotifications;
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
    private readonly IBusinessAuditSink _businessAuditSink;
    private readonly ICoinLedgerService _coinLedgerService;
    private readonly ICustomerNotificationService _customerNotificationService;
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
        TimeProvider timeProvider,
        ICustomerNotificationService? customerNotificationService = null,
        IBusinessAuditSink? businessAuditSink = null)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _businessAuditSink = businessAuditSink ?? NoopBusinessAuditSink.Instance;
        _coinLedgerService = coinLedgerService;
        _customerNotificationService = customerNotificationService ?? NullCustomerNotificationService.Instance;
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
            return await RejectedAsync(command, [BrandErrors.NotFound()], null, null, null, null, cancellationToken);

        if (!brand.IsCoinsEnabled)
            return await RejectedAsync(command, [BrandErrors.CoinsDisabled()], null, null, null, null, cancellationToken);

        if (!brand.IsCoinProductRedemptionEnabled)
            return await RejectedAsync(command, [BrandErrors.CoinProductRedemptionDisabled()], null, null, null, null, cancellationToken);

        var canRedeem = await _brandAccessService.CanAsync(
            command.RequestUserId,
            command.BrandId,
            PermissionCode.StampRedeem,
            cancellationToken);

        if (!canRedeem)
            return await RejectedAsync(command, [AccessErrors.Denied()], null, null, null, null, cancellationToken);

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null || product.BrandId != command.BrandId)
            return await RejectedAsync(command, [CoinProductErrors.NotFound()], null, null, null, null, cancellationToken);

        if (!product.IsActive)
            return await RejectedAsync(command, [CoinProductErrors.IsNotActive()], null, null, product.Price, null, cancellationToken);

        var code = command.RedemptionCode.Trim();
        if (!DomainRedemptionCode.IsValidCode(code))
            return await RejectedAsync(command, [UserErrors.RedemptionCodeInvalid()], null, null, product.Price, null, cancellationToken);

        var activeCode = await _redemptionCodeRepository.GetActiveByCodeAsync(
            code,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        if (activeCode is null)
            return await RejectedAsync(command, [UserErrors.RedemptionCodeNotFoundOrExpired()], null, null, product.Price, null, cancellationToken);

        var customer = await _userRepository.GetByIdAsync(activeCode.UserId, cancellationToken);
        if (customer is null)
            return await RejectedAsync(command, [UserErrors.NotFound()], activeCode.UserId, null, product.Price, null, cancellationToken);

        var wallet = await _coinWalletRepository.GetByUserAndBrandAsync(
            customer.Id,
            command.BrandId,
            cancellationToken);

        if (wallet is null)
            return await RejectedAsync(command, [CoinErrors.WalletNotFound()], customer.Id, null, product.Price, null, cancellationToken);

        var currentBalance = await _coinTransactionRepository.CalculateWalletValueAsync(
            wallet.Id,
            cancellationToken);

        if (currentBalance < product.Price)
            return await RejectedAsync(
                command,
                [CoinErrors.InsufficientFunds(currentBalance, product.Price)],
                customer.Id,
                currentBalance,
                product.Price,
                null,
                cancellationToken);

        var comment = product.Name;
        var transactionPrecheck = CoinTransaction.CreateRedeem(
            wallet.Id,
            product.Price,
            comment,
            command.RequestUserId);
        if (transactionPrecheck.IsFailed)
            return await RejectedAsync(command, transactionPrecheck.Errors, customer.Id, currentBalance, product.Price, comment, cancellationToken);

        var useCodeResult = await _useRedemptionCodeHandler.Handle(
            new UseRedemptionCodeCommand(code),
            cancellationToken);

        if (useCodeResult.IsFailed)
            return await RejectedAsync(command, useCodeResult.Errors, customer.Id, currentBalance, product.Price, comment, cancellationToken);

        var operationResult = await _coinLedgerService.RedeemAsync(
            customer.Id,
            command.RequestUserId,
            command.BrandId,
            product.Price,
            comment,
            cancellationToken);

        if (operationResult.IsFailed)
            return await RejectedAsync(command, operationResult.Errors, customer.Id, currentBalance, product.Price, comment, cancellationToken);

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

        await _customerNotificationService.NotifyCoinProductPurchasedAsync(response, product.Name, cancellationToken);
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.PurchaseCoinProduct,
                BusinessAuditOperationStatus.Succeeded,
                BrandId: command.BrandId,
                ActorUserId: command.RequestUserId,
                CustomerUserId: customer.Id,
                TargetEntityType: BusinessAuditTargetEntityType.CoinProduct,
                TargetEntityId: product.Id,
                Amount: product.Price,
                BalanceBefore: operation.BalanceBefore,
                BalanceAfter: operation.BalanceAfter,
                Comment: comment,
                Metadata: new Dictionary<string, object?>
                {
                    ["coinTransactionId"] = operation.Transaction.Id
                }),
            cancellationToken);

        return Result.Ok(response);
    }

    private async Task<Result<CoinOperationResponse>> RejectedAsync(
        PurchaseCoinProductCommand command,
        IReadOnlyCollection<IError> errors,
        Guid? customerUserId,
        int? balanceBefore,
        int? amount,
        string? comment,
        CancellationToken cancellationToken)
    {
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.PurchaseCoinProduct,
                BusinessAuditOperationStatus.Rejected,
                BrandId: command.BrandId,
                ActorUserId: command.RequestUserId,
                CustomerUserId: customerUserId,
                TargetEntityType: BusinessAuditTargetEntityType.CoinProduct,
                TargetEntityId: command.ProductId,
                Amount: amount,
                BalanceBefore: balanceBefore,
                ReasonCode: BusinessAuditReason.FromErrors(errors),
                Comment: comment),
            cancellationToken);

        return Result.Fail(errors);
    }
}
