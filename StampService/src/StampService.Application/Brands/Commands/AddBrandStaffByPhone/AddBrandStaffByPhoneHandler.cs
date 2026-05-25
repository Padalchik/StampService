using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Auth;
using StampService.Application.Brands;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.User;

namespace StampService.Application.Brands.Commands.AddBrandStaffByPhone;

public class AddBrandStaffByPhoneHandler
    : ICommandHandler<AddBrandStaffByPhoneResponse, AddBrandStaffByPhoneCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandMembershipService _brandMembershipService;
    private readonly IUserRepository _userRepository;

    public AddBrandStaffByPhoneHandler(
        IBrandAccessService brandAccessService,
        IBrandMembershipService brandMembershipService,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _brandMembershipService = brandMembershipService;
        _userRepository = userRepository;
    }

    public async Task<Result<AddBrandStaffByPhoneResponse>> Handle(
        AddBrandStaffByPhoneCommand command,
        CancellationToken cancellationToken)
    {
        var canManageStaff = await BrandStaffAuthorization.CanManageStaffAsync(
            _brandAccessService,
            command.ActorUserId,
            command.BrandId,
            cancellationToken);

        if (!canManageStaff)
            return Result.Fail(AccessErrors.Denied());

        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            command.PhoneNumber,
            nameof(command.PhoneNumber));
        if (phoneNumberResult.IsFailed)
            return Result.Fail(phoneNumberResult.Errors);

        var phoneNumber = phoneNumberResult.Value;
        var user = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            phoneNumber,
            cancellationToken);
        if (user is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        var membershipResult = await _brandMembershipService.AddStaffAsync(
            command.BrandId,
            user.Id,
            cancellationToken);

        if (membershipResult.IsFailed)
            return Result.Fail(membershipResult.Errors);

        var membership = membershipResult.Value;
        return Result.Ok(new AddBrandStaffByPhoneResponse(
            membership.BrandId,
            user.Id,
            user.Name,
            phoneNumber,
            membership.Id,
            membership.CreatedAt));
    }
}
