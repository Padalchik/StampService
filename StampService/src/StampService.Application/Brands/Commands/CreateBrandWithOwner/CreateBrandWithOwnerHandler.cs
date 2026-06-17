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
using BrandEntity = StampService.Domain.Brand.Brand;

namespace StampService.Application.Brands.Commands.CreateBrandWithOwner;

public class CreateBrandWithOwnerHandler : ICommandHandler<CreateBrandWithOwnerResponse, CreateBrandWithOwnerCommand>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IBrandCustomerService _brandCustomerService;
    private readonly IBrandRepository _brandRepository;
    private readonly IBrandMembershipRepository _brandMembershipRepository;
    private readonly IUserRepository _userRepository;

    public CreateBrandWithOwnerHandler(
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

    public async Task<Result<CreateBrandWithOwnerResponse>> Handle(
        CreateBrandWithOwnerCommand command,
        CancellationToken cancellationToken)
    {
        if (!await _adminAccessService.IsAdminAsync(command.Admin, cancellationToken))
            return Result.Fail(AccessErrors.AdminRequired());

        var ownerPhoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            command.OwnerPhoneNumber,
            nameof(command.OwnerPhoneNumber));
        if (ownerPhoneNumberResult.IsFailed)
            return Result.Fail(ownerPhoneNumberResult.Errors);

        var ownerPhoneNumber = ownerPhoneNumberResult.Value;
        var owner = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            ownerPhoneNumber,
            cancellationToken);
        if (owner is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        var ownerRole = await _brandMembershipRepository.GetRoleBySystemNameAsync(
            SystemRoles.Owner,
            cancellationToken);
        if (ownerRole is null)
            return Result.Fail(BrandErrors.OwnerRoleNotFound());

        var brandResult = BrandEntity.Create(command.BrandName);
        if (brandResult.IsFailed)
            return Result.Fail(brandResult.Errors);

        var brand = brandResult.Value;
        var membershipResult = BrandMembership.Create(owner.Id, brand.Id, ownerRole.Id);
        if (membershipResult.IsFailed)
            return Result.Fail(membershipResult.Errors);

        var membership = membershipResult.Value;
        var brandCustomerResult = await _brandCustomerService.EnsureAsync(
            brand.Id,
            owner.Id,
            owner.Id,
            cancellationToken);
        if (brandCustomerResult.IsFailed)
            return Result.Fail(brandCustomerResult.Errors);

        var addBrandResult = _brandRepository.Add(brand);
        if (addBrandResult.IsFailed)
            return Result.Fail(addBrandResult.Errors);

        _brandMembershipRepository.Add(membership);
        await _brandMembershipRepository.SaveAsync(cancellationToken);

        return Result.Ok(new CreateBrandWithOwnerResponse(
            brand.Id,
            brand.Name,
            brand.IsMetricsEnabled,
            brand.IsCoinsEnabled,
            brand.IsCoinProductRedemptionEnabled,
            brand.IsManualCoinRedemptionEnabled,
            owner.Id,
            owner.Name,
            ownerPhoneNumber,
            membership.Id,
            brand.CreatedAt));
    }
}
