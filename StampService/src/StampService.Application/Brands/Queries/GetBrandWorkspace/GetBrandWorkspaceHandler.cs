using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;

namespace StampService.Application.Brands.Queries.GetBrandWorkspace;

public class GetBrandWorkspaceHandler : IQueryHandler<BrandWorkspaceResponse, GetBrandWorkspaceQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandMembershipRepository _brandMembershipRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IUserRepository _userRepository;

    public GetBrandWorkspaceHandler(
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

    public async Task<Result<BrandWorkspaceResponse>> Handle(
        GetBrandWorkspaceQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        if (query.BrandId == Guid.Empty)
            return Result.Fail(BrandErrors.IdIsEmpty());

        var userExists = await _userRepository.ExistsAsync(query.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail(UserErrors.NotFound());

        var memberships = await _brandMembershipRepository.GetUserBrandMembershipsAsync(
            query.UserId,
            cancellationToken);

        var membership = memberships.FirstOrDefault(item => item.BrandId == query.BrandId);
        if (membership is null)
            return Result.Fail(BrandErrors.MembershipNotFound());

        var brand = await _brandRepository.GetByIdAsync(query.BrandId, cancellationToken);
        if (brand is null)
            return Result.Fail(BrandErrors.NotFound());

        var canIssue = await _brandAccessService.CanAsync(query.UserId, query.BrandId, PermissionCode.StampIssue, cancellationToken);
        var canRedeem = await _brandAccessService.CanAsync(query.UserId, query.BrandId, PermissionCode.StampRedeem, cancellationToken);
        var canViewBalances = await _brandAccessService.CanAsync(query.UserId, query.BrandId, PermissionCode.BalanceView, cancellationToken);
        var canManageBrand = await _brandAccessService.CanAsync(query.UserId, query.BrandId, PermissionCode.BrandManage, cancellationToken);
        var canManageMetrics = await _brandAccessService.CanAsync(query.UserId, query.BrandId, PermissionCode.MetricManage, cancellationToken);

        var response = new BrandWorkspaceResponse(
            query.BrandId,
            brand.Name,
            membership.RoleSystemName,
            brand.IsMetricsEnabled,
            brand.IsCoinsEnabled,
            canIssue,
            canRedeem,
            canViewBalances,
            canManageBrand,
            canManageMetrics,
            await _brandAccessService.CanAsync(query.UserId, query.BrandId, PermissionCode.StaffManage, cancellationToken));

        return Result.Ok(response);
    }
}
