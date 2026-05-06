using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;

namespace StampService.Application.Brands.Commands.AddBrandStaff;

public class AddBrandStaffHandler : ICommandHandler<AddBrandStaffResponse, AddBrandStaffCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandMembershipService _brandMembershipService;

    public AddBrandStaffHandler(
        IBrandAccessService brandAccessService,
        IBrandMembershipService brandMembershipService)
    {
        _brandAccessService = brandAccessService;
        _brandMembershipService = brandMembershipService;
    }

    public async Task<Result<AddBrandStaffResponse>> Handle(
        AddBrandStaffCommand command,
        CancellationToken cancellationToken)
    {
        var canManageStaff = await _brandAccessService.CanAsync(
            command.RequestUserId,
            command.BrandId,
            PermissionCode.StaffManage,
            cancellationToken);

        if (!canManageStaff)
            return Result.Fail("Access denied");

        var result = await _brandMembershipService.AddStaffAsync(
            command.BrandId,
            command.Request.UserId,
            cancellationToken);

        if (result.IsFailed)
            return Result.Fail(result.Errors);

        var membership = result.Value;

        return Result.Ok(new AddBrandStaffResponse(
            membership.Id,
            membership.BrandId,
            membership.UserId,
            SystemRoles.Staff,
            membership.CreatedAt));
    }
}
