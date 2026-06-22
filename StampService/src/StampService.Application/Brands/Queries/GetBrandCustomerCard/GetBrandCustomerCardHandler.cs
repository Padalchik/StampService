using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Auth;
using StampService.Application.Brands;
using StampService.Application.Errors;
using StampService.Application.Wallet.Queries.GetUserWalletBrandDetails;
using StampService.Contracts.DTOs.Brands;
using StampService.Contracts.DTOs.Wallet;
using StampService.Domain.Access;
using StampService.Domain.User;

namespace StampService.Application.Brands.Queries.GetBrandCustomerCard;

public class GetBrandCustomerCardHandler
    : IQueryHandler<BrandCustomerCardResponse, GetBrandCustomerCardQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandCustomerRepository _brandCustomerRepository;
    private readonly IQueryHandler<UserWalletBrandDetailsResponse, GetUserWalletBrandDetailsQuery> _walletDetailsHandler;

    public GetBrandCustomerCardHandler(
        IBrandAccessService brandAccessService,
        IBrandCustomerRepository brandCustomerRepository,
        IQueryHandler<UserWalletBrandDetailsResponse, GetUserWalletBrandDetailsQuery> walletDetailsHandler)
    {
        _brandAccessService = brandAccessService;
        _brandCustomerRepository = brandCustomerRepository;
        _walletDetailsHandler = walletDetailsHandler;
    }

    public async Task<Result<BrandCustomerCardResponse>> Handle(
        GetBrandCustomerCardQuery query,
        CancellationToken cancellationToken)
    {
        if (query.RequestUserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        if (query.BrandId == Guid.Empty)
            return Result.Fail(BrandErrors.IdIsEmpty());

        var canViewBalances = await _brandAccessService.CanAsync(
            query.RequestUserId,
            query.BrandId,
            PermissionCode.BalanceView,
            cancellationToken);

        if (!canViewBalances)
            return Result.Fail(AccessErrors.Denied());

        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            query.CustomerPhoneNumber,
            nameof(query.CustomerPhoneNumber));
        if (phoneNumberResult.IsFailed)
            return Result.Fail(phoneNumberResult.Errors);

        var customer = await _brandCustomerRepository.GetCustomerByPhoneAsync(
            query.BrandId,
            IdentityType.Phone,
            phoneNumberResult.Value,
            cancellationToken);
        if (customer is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        var detailsResult = await _walletDetailsHandler.Handle(
            new GetUserWalletBrandDetailsQuery(customer.Id, query.BrandId),
            cancellationToken);
        if (detailsResult.IsFailed)
            return Result.Fail(detailsResult.Errors);

        return Result.Ok(new BrandCustomerCardResponse(
            query.BrandId,
            customer.Id,
            customer.Name,
            phoneNumberResult.Value,
            detailsResult.Value));
    }
}
