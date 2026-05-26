using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Brands.Commands.CreateBrandWithOwner;
using StampService.Application.Brands.Commands.ReassignBrandOwner;
using StampService.Application.Brands.Queries.GetAdminBrands;
using StampService.Contracts.DTOs.Brands;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController : ApiControllerBase
{
    [HttpGet("brands")]
    public async Task<EndpointResult<IReadOnlyCollection<AdminBrandResponse>>> GetBrands(
        [FromServices] IQueryHandler<IReadOnlyCollection<AdminBrandResponse>, GetAdminBrandsQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<IReadOnlyCollection<AdminBrandResponse>>();

        return await handler.Handle(
            new GetAdminBrandsQuery(AdminActor.FromUser(userIdResult.Value)),
            cancellationToken);
    }

    [HttpPost("brands")]
    public async Task<EndpointResult<CreateBrandWithOwnerResponse>> CreateBrand(
        CreateBrandWithOwnerRequest request,
        [FromServices] ICommandHandler<CreateBrandWithOwnerResponse, CreateBrandWithOwnerCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CreateBrandWithOwnerResponse>();

        return EndpointResult<CreateBrandWithOwnerResponse>.Created(
            await handler.Handle(
                new CreateBrandWithOwnerCommand(
                    AdminActor.FromUser(userIdResult.Value),
                    request.BrandName,
                    request.OwnerPhoneNumber),
                cancellationToken));
    }

    [HttpPut("brands/{brandId:guid}/owner")]
    public async Task<EndpointResult<ReassignBrandOwnerResponse>> ReassignOwner(
        Guid brandId,
        ReassignBrandOwnerRequest request,
        [FromServices] ICommandHandler<ReassignBrandOwnerResponse, ReassignBrandOwnerCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<ReassignBrandOwnerResponse>();

        return await handler.Handle(
            new ReassignBrandOwnerCommand(
                AdminActor.FromUser(userIdResult.Value),
                brandId,
                request.NewOwnerPhoneNumber),
            cancellationToken);
    }
}
