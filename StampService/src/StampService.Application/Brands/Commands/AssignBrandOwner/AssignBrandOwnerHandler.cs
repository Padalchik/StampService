using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;

namespace StampService.Application.Brands.Commands.AssignBrandOwner;

public class AssignBrandOwnerHandler : ICommandHandler<AssignBrandOwnerResponse, AssignBrandOwnerCommand>
{
    private readonly IBrandMembershipService _brandMembershipService;

    public AssignBrandOwnerHandler(IBrandMembershipService brandMembershipService)
    {
        _brandMembershipService = brandMembershipService;
    }

    public async Task<Result<AssignBrandOwnerResponse>> Handle(
        AssignBrandOwnerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _brandMembershipService.AssignOwnerAsync(
            command.BrandId,
            command.Request.UserId,
            cancellationToken);

        if (result.IsFailed)
            return Result.Fail(result.Errors);

        var membership = result.Value;
        var response = new AssignBrandOwnerResponse(
            membership.Id,
            membership.BrandId,
            membership.UserId,
            SystemRoles.Owner,
            membership.CreatedAt);

        return Result.Ok(response);
    }
}
