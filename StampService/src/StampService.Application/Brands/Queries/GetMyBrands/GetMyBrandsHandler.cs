using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Brands;

namespace StampService.Application.Brands.Queries.GetMyBrands;

public class GetMyBrandsHandler : IQueryHandler<MyBrandsResponse, GetMyBrandsQuery>
{
    private readonly IBrandMembershipRepository _brandMembershipRepository;
    private readonly IUserRepository _userRepository;

    public GetMyBrandsHandler(
        IBrandMembershipRepository brandMembershipRepository,
        IUserRepository userRepository)
    {
        _brandMembershipRepository = brandMembershipRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<MyBrandsResponse>> Handle(
        GetMyBrandsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var userExists = await _userRepository.ExistsAsync(query.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(UserErrors.NotFound());

        var memberships = await _brandMembershipRepository.GetUserBrandMembershipsAsync(
            query.UserId,
            cancellationToken);

        var response = new MyBrandsResponse(
            query.UserId,
            memberships
                .Select(membership => new MyBrandResponse(
                    membership.BrandId,
                    membership.BrandName,
                    membership.RoleSystemName))
                .ToArray());

        return Result.Ok(response);
    }
}
