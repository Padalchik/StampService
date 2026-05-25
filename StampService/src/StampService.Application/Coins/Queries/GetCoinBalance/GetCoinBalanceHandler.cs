using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Auth;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Coins;
using StampService.Domain.Access;
using StampService.Domain.User;

namespace StampService.Application.Coins.Queries.GetCoinBalance;

public class GetCoinBalanceHandler : IQueryHandler<CoinBalanceResponse, GetCoinBalanceQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ICoinWalletRepository _coinWalletRepository;
    private readonly IUserRepository _userRepository;

    public GetCoinBalanceHandler(
        IBrandAccessService brandAccessService,
        ICoinWalletRepository coinWalletRepository,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _coinWalletRepository = coinWalletRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<CoinBalanceResponse>> Handle(
        GetCoinBalanceQuery query,
        CancellationToken cancellationToken)
    {
        var canViewBalance = await _brandAccessService.CanAsync(
            query.RequestUserId,
            query.BrandId,
            PermissionCode.BalanceView,
            cancellationToken);

        if (!canViewBalance)
            return Result.Fail(AccessErrors.Denied());

        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            query.CustomerPhoneNumber,
            nameof(query.CustomerPhoneNumber));
        if (phoneNumberResult.IsFailed)
            return Result.Fail(phoneNumberResult.Errors);

        var customer = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            phoneNumberResult.Value,
            cancellationToken);
        if (customer is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        var wallet = await _coinWalletRepository.GetByUserAndBrandAsync(
            customer.Id,
            query.BrandId,
            cancellationToken);

        return Result.Ok(new CoinBalanceResponse(
            wallet?.Id,
            query.BrandId,
            customer.Id,
            customer.Name,
            phoneNumberResult.Value,
            wallet?.Value ?? 0));
    }
}
