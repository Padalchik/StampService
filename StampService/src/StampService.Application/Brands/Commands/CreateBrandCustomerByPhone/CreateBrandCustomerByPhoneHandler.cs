using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands.Queries.GetBrandCustomerCard;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;

namespace StampService.Application.Brands.Commands.CreateBrandCustomerByPhone;

public class CreateBrandCustomerByPhoneHandler
    : ICommandHandler<BrandCustomerCardResponse, CreateBrandCustomerByPhoneCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IQueryHandler<BrandCustomerCardResponse, GetBrandCustomerCardQuery> _customerCardHandler;
    private readonly IPhoneAccountService _phoneAccountService;
    private readonly IUserRepository _userRepository;

    public CreateBrandCustomerByPhoneHandler(
        IBrandAccessService brandAccessService,
        IPhoneAccountService phoneAccountService,
        IUserRepository userRepository,
        IQueryHandler<BrandCustomerCardResponse, GetBrandCustomerCardQuery> customerCardHandler)
    {
        _brandAccessService = brandAccessService;
        _customerCardHandler = customerCardHandler;
        _phoneAccountService = phoneAccountService;
        _userRepository = userRepository;
    }

    public async Task<Result<BrandCustomerCardResponse>> Handle(
        CreateBrandCustomerByPhoneCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        if (command.BrandId == Guid.Empty)
            return Result.Fail(BrandErrors.IdIsEmpty());

        var canViewBalances = await _brandAccessService.CanAsync(
            command.ActorUserId,
            command.BrandId,
            PermissionCode.BalanceView,
            cancellationToken);

        if (!canViewBalances)
            return Result.Fail(AccessErrors.Denied());

        var customerResult = await _phoneAccountService.GetOrCreateForBusinessOperationAsync(
            command.Request.PhoneNumber,
            nameof(command.Request.PhoneNumber),
            cancellationToken);
        if (customerResult.IsFailed)
            return Result.Fail(customerResult.Errors);

        await _userRepository.SaveAsync(cancellationToken);

        return await _customerCardHandler.Handle(
            new GetBrandCustomerCardQuery(
                command.ActorUserId,
                command.BrandId,
                command.Request.PhoneNumber),
            cancellationToken);
    }
}
