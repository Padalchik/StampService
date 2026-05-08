using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Brands;
using UserEntity = StampService.Domain.User.User;

namespace StampService.Application.Brands.Commands.AddBrandStaffByCustomerCode;

public class AddBrandStaffByCustomerCodeHandler
    : ICommandHandler<AddBrandStaffByCustomerCodeResponse, AddBrandStaffByCustomerCodeCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandMembershipService _brandMembershipService;
    private readonly IUserRepository _userRepository;

    public AddBrandStaffByCustomerCodeHandler(
        IBrandAccessService brandAccessService,
        IBrandMembershipService brandMembershipService,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _brandMembershipService = brandMembershipService;
        _userRepository = userRepository;
    }

    public async Task<Result<AddBrandStaffByCustomerCodeResponse>> Handle(
        AddBrandStaffByCustomerCodeCommand command,
        CancellationToken cancellationToken)
    {
        var canManageStaff = await BrandStaffAuthorization.CanManageStaffAsync(
            _brandAccessService,
            command.ActorUserId,
            command.BrandId,
            cancellationToken);

        if (!canManageStaff)
            return Result.Fail(AccessErrors.Denied());

        var customerCode = command.CustomerCode.Trim();
        if (!UserEntity.IsValidCustomerCode(customerCode))
            return Result.Fail(UserErrors.CustomerCodeInvalid());

        var user = await _userRepository.GetByCustomerCodeAsync(customerCode, cancellationToken);
        if (user is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        var membershipResult = await _brandMembershipService.AddStaffAsync(
            command.BrandId,
            user.Id,
            cancellationToken);

        if (membershipResult.IsFailed)
            return Result.Fail(membershipResult.Errors);

        var membership = membershipResult.Value;
        return Result.Ok(new AddBrandStaffByCustomerCodeResponse(
            membership.BrandId,
            user.Id,
            user.Name,
            user.CustomerCode,
            membership.Id,
            membership.CreatedAt));
    }
}
