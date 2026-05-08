using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;

namespace StampService.Application.Brands.Commands.RemoveBrandStaff;

public class RemoveBrandStaffHandler : ICommandHandler<RemoveBrandStaffResponse, RemoveBrandStaffCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandMembershipRepository _brandMembershipRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IUserRepository _userRepository;

    public RemoveBrandStaffHandler(
        IBrandAccessService brandAccessService,
        IBrandMembershipRepository brandMembershipRepository,
        IBrandRepository brandRepository,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _brandMembershipRepository = brandMembershipRepository;
        _brandRepository = brandRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<RemoveBrandStaffResponse>> Handle(
        RemoveBrandStaffCommand command,
        CancellationToken cancellationToken)
    {
        if (command.BrandId == Guid.Empty)
            return Result.Fail(BrandErrors.IdIsEmpty());

        var brandExists = await _brandRepository.ExistsAsync(command.BrandId, cancellationToken);
        if (!brandExists)
            return Result.Fail(BrandErrors.NotFound());

        var canManageStaff = await BrandStaffAuthorization.CanManageStaffAsync(
            _brandAccessService,
            command.ActorUserId,
            command.BrandId,
            cancellationToken);

        if (!canManageStaff)
            return Result.Fail(AccessErrors.Denied());

        var user = await _userRepository.GetByIdAsync(command.StaffUserId, cancellationToken);
        if (user is null)
            return Result.Fail(UserErrors.NotFound());

        var currentRole = await _brandMembershipRepository.GetRoleSystemNameAsync(
            command.StaffUserId,
            command.BrandId,
            cancellationToken);

        if (currentRole == SystemRoles.Owner)
            return Result.Fail(BrandErrors.CannotChangeOwnerRole());

        if (currentRole != SystemRoles.Staff)
            return Result.Fail(BrandErrors.MembershipNotFound());

        var membership = await _brandMembershipRepository.GetByBrandAndUserAsync(
            command.BrandId,
            command.StaffUserId,
            cancellationToken);

        if (membership is null)
            return Result.Fail(BrandErrors.MembershipNotFound());

        _brandMembershipRepository.Remove(membership);
        await _brandMembershipRepository.SaveAsync(cancellationToken);

        return Result.Ok(new RemoveBrandStaffResponse(
            command.BrandId,
            user.Id,
            user.Name,
            user.CustomerCode));
    }
}
