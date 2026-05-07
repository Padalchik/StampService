using Microsoft.EntityFrameworkCore;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Domain.Access;

namespace StampService.Infrastructure.Repositories;

public class BrandMembershipRepository : IBrandMembershipRepository
{
    private readonly AppDbContext _dbContext;

    public BrandMembershipRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetRoleSystemNameAsync(
        Guid userId,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BrandMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId && membership.BrandId == brandId)
            .Select(membership => membership.Role.SystemName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserBrandMembershipReadModel>> GetUserBrandMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BrandMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .OrderBy(membership => membership.Brand.Name)
            .Select(membership => new UserBrandMembershipReadModel(
                membership.BrandId,
                membership.Brand.Name,
                membership.Role.SystemName))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<BrandMembership?> GetByBrandAndUserAsync(
        Guid brandId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BrandMemberships
            .FirstOrDefaultAsync(
                membership => membership.BrandId == brandId && membership.UserId == userId,
                cancellationToken);
    }

    public async Task<BrandMembership?> GetOwnerAsync(
        Guid brandId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BrandMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(
                membership => membership.BrandId == brandId
                    && membership.Role.SystemName == SystemRoles.Owner,
                cancellationToken);
    }

    public async Task<Role?> GetRoleBySystemNameAsync(
        string systemName,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Roles
            .FirstOrDefaultAsync(role => role.SystemName == systemName, cancellationToken);
    }

    public void Add(BrandMembership membership)
    {
        _dbContext.BrandMemberships.Add(membership);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
