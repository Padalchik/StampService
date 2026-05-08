using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Administration;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;
using UserEntity = StampService.Domain.User.User;

namespace StampService.Application.Brands.Commands.ReassignBrandOwner;

public class ReassignBrandOwnerHandler : ICommandHandler<ReassignBrandOwnerResponse, ReassignBrandOwnerCommand>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly IBrandMembershipRepository _brandMembershipRepository;
    private readonly IUserRepository _userRepository;

    public ReassignBrandOwnerHandler(
        IAdminAccessService adminAccessService,
        IBrandRepository brandRepository,
        IBrandMembershipRepository brandMembershipRepository,
        IUserRepository userRepository)
    {
        _adminAccessService = adminAccessService;
        _brandRepository = brandRepository;
        _brandMembershipRepository = brandMembershipRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<ReassignBrandOwnerResponse>> Handle(
        ReassignBrandOwnerCommand command,
        CancellationToken cancellationToken)
    {
        if (!_adminAccessService.IsAdmin(command.AdminTelegramUserId))
            return Result.Fail(AccessErrors.AdminRequired());

        var brandExists = await _brandRepository.ExistsAsync(command.BrandId, cancellationToken);
        if (!brandExists)
            return Result.Fail(BrandErrors.NotFound());

        var ownerCustomerCode = command.NewOwnerCustomerCode.Trim();
        if (!UserEntity.IsValidCustomerCode(ownerCustomerCode))
            return Result.Fail(UserErrors.CustomerCodeInvalid());

        var newOwner = await _userRepository.GetByCustomerCodeAsync(ownerCustomerCode, cancellationToken);
        if (newOwner is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        var ownerRole = await _brandMembershipRepository.GetRoleBySystemNameAsync(
            SystemRoles.Owner,
            cancellationToken);
        if (ownerRole is null)
            return Result.Fail(BrandErrors.OwnerRoleNotFound());

        var currentOwner = await _brandMembershipRepository.GetOwnerAsync(command.BrandId, cancellationToken);
        if (currentOwner is not null && currentOwner.UserId == newOwner.Id)
        {
            return Result.Ok(new ReassignBrandOwnerResponse(
                command.BrandId,
                newOwner.Id,
                newOwner.Name,
                newOwner.CustomerCode,
                currentOwner.Id,
                null));
        }

        var removedOwnerUserId = currentOwner?.UserId;
        if (currentOwner is not null)
            _brandMembershipRepository.Remove(currentOwner);

        var newOwnerMembership = await _brandMembershipRepository.GetByBrandAndUserAsync(
            command.BrandId,
            newOwner.Id,
            cancellationToken);

        if (newOwnerMembership is null)
        {
            var membershipResult = BrandMembership.Create(newOwner.Id, command.BrandId, ownerRole.Id);
            if (membershipResult.IsFailed)
                return Result.Fail(membershipResult.Errors);

            newOwnerMembership = membershipResult.Value;
            _brandMembershipRepository.Add(newOwnerMembership);
        }
        else
        {
            var changeRoleResult = newOwnerMembership.ChangeRole(ownerRole.Id);
            if (changeRoleResult.IsFailed)
                return Result.Fail(changeRoleResult.Errors);
        }

        await _brandMembershipRepository.SaveAsync(cancellationToken);

        return Result.Ok(new ReassignBrandOwnerResponse(
            command.BrandId,
            newOwner.Id,
            newOwner.Name,
            newOwner.CustomerCode,
            newOwnerMembership.Id,
            removedOwnerUserId));
    }
}
