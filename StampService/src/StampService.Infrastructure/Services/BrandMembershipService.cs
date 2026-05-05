using FluentResults;
using Microsoft.EntityFrameworkCore;
using StampService.Application.Access;
using StampService.Domain.Access;

namespace StampService.Infrastructure.Services;

public class BrandMembershipService : IBrandMembershipService
{
    private readonly AppDbContext _dbContext;

    public BrandMembershipService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BrandMembership>> AssignOwnerAsync(
        Guid brandId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var brandExists = await _dbContext.Brands
            .AnyAsync(brand => brand.Id == brandId, cancellationToken);
        if (!brandExists)
            return Result.Fail("Brand not found");

        var userExists = await _dbContext.Users
            .AnyAsync(user => user.Id == userId, cancellationToken);
        if (!userExists)
            return Result.Fail("User not found");

        var ownerRole = await _dbContext.Roles
            .FirstOrDefaultAsync(role => role.SystemName == SystemRoles.Owner, cancellationToken);
        if (ownerRole is null)
            return Result.Fail("Owner role not found");

        var existingOwner = await _dbContext.BrandMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.BrandId == brandId && item.RoleId == ownerRole.Id,
                cancellationToken);

        if (existingOwner is not null && existingOwner.UserId != userId)
            return Result.Fail("Brand already has an owner");

        var membership = await _dbContext.BrandMemberships
            .FirstOrDefaultAsync(
                item => item.BrandId == brandId && item.UserId == userId,
                cancellationToken);

        if (membership is null)
        {
            var membershipResult = BrandMembership.Create(userId, brandId, ownerRole.Id);
            if (membershipResult.IsFailed)
                return Result.Fail(membershipResult.Errors);

            membership = membershipResult.Value;
            _dbContext.BrandMemberships.Add(membership);
        }
        else
        {
            var changeRoleResult = membership.ChangeRole(ownerRole.Id);
            if (changeRoleResult.IsFailed)
                return Result.Fail(changeRoleResult.Errors);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(membership);
    }
}
