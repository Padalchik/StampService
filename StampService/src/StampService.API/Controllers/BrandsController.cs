using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Commands.AddBrandStaffByPhone;
using StampService.Application.Brands.Commands.RemoveBrandStaff;
using StampService.Application.Brands.Commands.UpdateBrandRewardSettings;
using StampService.Application.Brands.Queries.GetBrandStaff;
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

    [HttpGet("{brandId:guid}/staff")]
    public async Task<EndpointResult<IReadOnlyCollection<BrandStaffResponse>>> GetStaff(
        Guid brandId,
        [FromServices] IQueryHandler<IReadOnlyCollection<BrandStaffResponse>, GetBrandStaffQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<IReadOnlyCollection<BrandStaffResponse>>();

        return await handler.Handle(
            new GetBrandStaffQuery(userIdResult.Value, brandId),
            cancellationToken);
    }

    [HttpPut("{brandId:guid}/reward-settings")]
    public async Task<EndpointResult<UpdateBrandResponse>> UpdateRewardSettings(
        Guid brandId,
        UpdateBrandRewardSettingsRequest request,
        [FromServices] ICommandHandler<UpdateBrandResponse, UpdateBrandRewardSettingsCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<UpdateBrandResponse>();

        return await handler.Handle(
            new UpdateBrandRewardSettingsCommand(
                userIdResult.Value,
                brandId,
                request.IsMetricsEnabled,
                request.IsCoinsEnabled,
                request.IsCoinProductRedemptionEnabled,
                request.IsManualCoinRedemptionEnabled),
            cancellationToken);
    }

    [HttpPost("{brandId:guid}/staff/by-phone")]
    public async Task<EndpointResult<AddBrandStaffByPhoneResponse>> AddStaffByPhone(
        Guid brandId,
        AddBrandStaffByPhoneRequest request,
        [FromServices] ICommandHandler<AddBrandStaffByPhoneResponse, AddBrandStaffByPhoneCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<AddBrandStaffByPhoneResponse>();

        return await handler.Handle(
            new AddBrandStaffByPhoneCommand(
                userIdResult.Value,
                brandId,
                request.PhoneNumber),
            cancellationToken);
    }

    [HttpDelete("{brandId:guid}/staff/{staffUserId:guid}")]
    public async Task<EndpointResult<RemoveBrandStaffResponse>> RemoveStaff(
        Guid brandId,
        Guid staffUserId,
        [FromServices] ICommandHandler<RemoveBrandStaffResponse, RemoveBrandStaffCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<RemoveBrandStaffResponse>();

        return await handler.Handle(
            new RemoveBrandStaffCommand(
                userIdResult.Value,
                brandId,
                staffUserId),
            cancellationToken);
    }
}
