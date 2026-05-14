using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Administration;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;
using BrandEntity = StampService.Domain.Brand.Brand;
using UserEntity = StampService.Domain.User.User;

namespace StampService.Application.Brands.Commands.CreateBrandWithOwner;

public class CreateBrandWithOwnerHandler : ICommandHandler<CreateBrandWithOwnerResponse, CreateBrandWithOwnerCommand>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly IBrandMembershipRepository _brandMembershipRepository;
    private readonly IUserRepository _userRepository;

    public CreateBrandWithOwnerHandler(
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

    public async Task<Result<CreateBrandWithOwnerResponse>> Handle(
        CreateBrandWithOwnerCommand command,
        CancellationToken cancellationToken)
    {
        if (!_adminAccessService.IsAdmin(command.AdminTelegramUserId))
            return Result.Fail(AccessErrors.AdminRequired());

        var ownerCustomerCode = command.OwnerCustomerCode.Trim();
        if (!UserEntity.IsValidCustomerCode(ownerCustomerCode))
            return Result.Fail(UserErrors.CustomerCodeInvalid());

        var owner = await _userRepository.GetByCustomerCodeAsync(ownerCustomerCode, cancellationToken);
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
        var addBrandResult = await _brandRepository.AddAsync(brand, cancellationToken);
        if (addBrandResult.IsFailed)
            return Result.Fail(addBrandResult.Errors);

        var membershipResult = BrandMembership.Create(owner.Id, brand.Id, ownerRole.Id);
        if (membershipResult.IsFailed)
            return Result.Fail(membershipResult.Errors);

        var membership = membershipResult.Value;
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
            owner.CustomerCode,
            membership.Id,
            brand.CreatedAt));
    }
}
