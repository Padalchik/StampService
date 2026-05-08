using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Brands;

namespace StampService.Application.Brands.Queries.GetBrandStaff;

public class GetBrandStaffHandler : IQueryHandler<IReadOnlyCollection<BrandStaffResponse>, GetBrandStaffQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandMembershipRepository _brandMembershipRepository;
    private readonly IBrandRepository _brandRepository;

    public GetBrandStaffHandler(
        IBrandAccessService brandAccessService,
        IBrandMembershipRepository brandMembershipRepository,
        IBrandRepository brandRepository)
    {
        _brandAccessService = brandAccessService;
        _brandMembershipRepository = brandMembershipRepository;
        _brandRepository = brandRepository;
    }

    public async Task<Result<IReadOnlyCollection<BrandStaffResponse>>> Handle(
        GetBrandStaffQuery query,
        CancellationToken cancellationToken)
    {
        if (query.BrandId == Guid.Empty)
            return Result.Fail(BrandErrors.IdIsEmpty());

        var brandExists = await _brandRepository.ExistsAsync(query.BrandId, cancellationToken);
        if (!brandExists)
            return Result.Fail(BrandErrors.NotFound());

        var canManageStaff = await BrandStaffAuthorization.CanManageStaffAsync(
            _brandAccessService,
            query.ActorUserId,
            query.BrandId,
            cancellationToken);

        if (!canManageStaff)
            return Result.Fail(AccessErrors.Denied());

        var staff = await _brandMembershipRepository.GetBrandStaffAsync(query.BrandId, cancellationToken);
        IReadOnlyCollection<BrandStaffResponse> response = staff
            .Select(item => new BrandStaffResponse(
                item.UserId,
                item.UserName,
                item.CustomerCode,
                item.MembershipCreatedAt))
            .ToArray();

        return Result.Ok(response);
    }
}
