using FluentResults;
using StampService.Domain.Access;

namespace StampService.Application.Access;

public interface IBrandMembershipService
{
    Task<Result<BrandMembership>> AssignOwnerAsync(
        Guid brandId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<Result<BrandMembership>> AddStaffAsync(
        Guid brandId,
        Guid userId,
        CancellationToken cancellationToken);
}
