using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Commands.AddBrandStaff;
using StampService.Contracts.DTOs.Brands;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BrandsController : ApiControllerBase
{
    [HttpPost("{brandId:guid}/staff")]
    public async Task<EndpointResult<AddBrandStaffResponse>> AddStaff(
        Guid brandId,
        AddBrandStaffRequest request,
        [FromServices] ICommandHandler<AddBrandStaffResponse, AddBrandStaffCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<AddBrandStaffResponse>();

        var command = new AddBrandStaffCommand(brandId, userIdResult.Value, request);

        return await handler.Handle(command, cancellationToken);
    }
}
