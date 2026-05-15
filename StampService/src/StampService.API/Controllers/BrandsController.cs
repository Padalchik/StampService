using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Commands.AddBrandStaff;
using StampService.Application.Brands.Commands.AssignBrandOwner;
using StampService.Application.Brands.Commands.CreateBrand;
using StampService.Contracts.DTOs.Brands;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BrandsController : ApiControllerBase
{
    [HttpPost]
    public async Task<EndpointResult<CreateBrandResponse>> Create(
        CreateBrandRequest request,
        [FromServices] ICommandHandler<CreateBrandResponse, CreateBrandCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateBrandCommand(request);

        return EndpointResult<CreateBrandResponse>.Created(
            await handler.Handle(command, cancellationToken));
    }

    [HttpPost("{brandId:guid}/owner")]
    public async Task<EndpointResult<AssignBrandOwnerResponse>> AssignOwner(
        Guid brandId,
        AssignBrandOwnerRequest request,
        [FromServices] ICommandHandler<AssignBrandOwnerResponse, AssignBrandOwnerCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new AssignBrandOwnerCommand(brandId, request);

        return await handler.Handle(command, cancellationToken);
    }

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
