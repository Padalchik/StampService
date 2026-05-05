using FluentResults;
using StampService.Application.Brands;
using StampService.Application.Users;
using StampService.Domain.Access;

namespace StampService.Application.Access;

public class BrandMembershipService : IBrandMembershipService
{
    private readonly IBrandRepository _brandRepository;
    private readonly IBrandMembershipRepository _brandMembershipRepository;
    private readonly IUserRepository _userRepository;

    public BrandMembershipService(
        IBrandRepository brandRepository,
        IBrandMembershipRepository brandMembershipRepository,
        IUserRepository userRepository)
    {
        _brandRepository = brandRepository;
        _brandMembershipRepository = brandMembershipRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<BrandMembership>> AssignOwnerAsync(
        Guid brandId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var brandExists = await _brandRepository.ExistsAsync(brandId, cancellationToken);
        if (!brandExists)
            return Result.Fail("Brand not found");

        var userExists = await _userRepository.ExistsAsync(userId, cancellationToken);
        if (!userExists)
            return Result.Fail("User not found");

        var ownerRole = await _brandMembershipRepository.GetRoleBySystemNameAsync(
            SystemRoles.Owner,
            cancellationToken);
        if (ownerRole is null)
            return Result.Fail("Owner role not found");

        var existingOwner = await _brandMembershipRepository.GetOwnerAsync(brandId, cancellationToken);
        if (existingOwner is not null && existingOwner.UserId != userId)
            return Result.Fail("Brand already has an owner");

        var membership = await _brandMembershipRepository.GetByBrandAndUserAsync(
            brandId,
            userId,
            cancellationToken);

        if (membership is null)
        {
            var membershipResult = BrandMembership.Create(userId, brandId, ownerRole.Id);
            if (membershipResult.IsFailed)
                return Result.Fail(membershipResult.Errors);

            membership = membershipResult.Value;
            _brandMembershipRepository.Add(membership);
        }
        else
        {
            var changeRoleResult = membership.ChangeRole(ownerRole.Id);
            if (changeRoleResult.IsFailed)
                return Result.Fail(changeRoleResult.Errors);
        }

        await _brandMembershipRepository.SaveAsync(cancellationToken);

        return Result.Ok(membership);
    }
}
