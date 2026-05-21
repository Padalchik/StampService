using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Commands.AddBrandStaff;
using StampService.Application.Brands.Queries.GetBrandWorkspace;
using StampService.Application.Brands.Queries.GetMyBrands;
using StampService.Contracts.DTOs.Brands;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BrandsController : ApiControllerBase
{
    [HttpGet("mine")]
    public async Task<EndpointResult<MyBrandsResponse>> GetMine(
        [FromServices] IQueryHandler<MyBrandsResponse, GetMyBrandsQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<MyBrandsResponse>();

        return await handler.Handle(
            new GetMyBrandsQuery(userIdResult.Value),
            cancellationToken);
    }

    [HttpGet("{brandId:guid}/workspace")]
    public async Task<EndpointResult<BrandWorkspaceResponse>> GetWorkspace(
        Guid brandId,
        [FromServices] IQueryHandler<BrandWorkspaceResponse, GetBrandWorkspaceQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<BrandWorkspaceResponse>();

        return await handler.Handle(
            new GetBrandWorkspaceQuery(userIdResult.Value, brandId),
            cancellationToken);
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
