using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Administration;
using StampService.Application.Auth;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;
using StampService.Domain.User;

namespace StampService.Application.Brands.Commands.ReassignBrandOwner;

public class ReassignBrandOwnerHandler : ICommandHandler<ReassignBrandOwnerResponse, ReassignBrandOwnerCommand>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IBrandCustomerService _brandCustomerService;
    private readonly IBrandRepository _brandRepository;
    private readonly IBrandMembershipRepository _brandMembershipRepository;
    private readonly IUserRepository _userRepository;

    public ReassignBrandOwnerHandler(
        IAdminAccessService adminAccessService,
        IBrandCustomerService brandCustomerService,
        IBrandRepository brandRepository,
        IBrandMembershipRepository brandMembershipRepository,
        IUserRepository userRepository)
    {
        _adminAccessService = adminAccessService;
        _brandCustomerService = brandCustomerService;
        _brandRepository = brandRepository;
        _brandMembershipRepository = brandMembershipRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<ReassignBrandOwnerResponse>> Handle(
        ReassignBrandOwnerCommand command,
        CancellationToken cancellationToken)
    {
        if (!await _adminAccessService.IsAdminAsync(command.Admin, cancellationToken))
            return Result.Fail(AccessErrors.AdminRequired());

        var brandExists = await _brandRepository.ExistsAsync(command.BrandId, cancellationToken);
        if (!brandExists)
            return Result.Fail(BrandErrors.NotFound());

        var ownerPhoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            command.NewOwnerPhoneNumber,
            nameof(command.NewOwnerPhoneNumber));
        if (ownerPhoneNumberResult.IsFailed)
            return Result.Fail(ownerPhoneNumberResult.Errors);

        var ownerPhoneNumber = ownerPhoneNumberResult.Value;
        var newOwner = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            ownerPhoneNumber,
            cancellationToken);
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
                ownerPhoneNumber,
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

        var brandCustomerResult = await _brandCustomerService.EnsureAsync(
            command.BrandId,
            newOwner.Id,
            newOwner.Id,
            cancellationToken);
        if (brandCustomerResult.IsFailed)
            return Result.Fail(brandCustomerResult.Errors);

        await _brandMembershipRepository.SaveAsync(cancellationToken);

        return Result.Ok(new ReassignBrandOwnerResponse(
            command.BrandId,
            newOwner.Id,
            newOwner.Name,
            ownerPhoneNumber,
            newOwnerMembership.Id,
            removedOwnerUserId));
    }
}
