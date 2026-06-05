using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentResults;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Audit.Queries.GetBusinessAuditLogs;
using StampService.Application.Brands.Commands.CreateBrandWithOwner;
using StampService.Application.Brands.Commands.ReassignBrandOwner;
using StampService.Application.Brands.Queries.GetAdminBrands;
using StampService.Application.Demo.Commands.CreateDemoBrands;
using StampService.Application.Demo.Commands.CreateUserDemoData;
using StampService.Application.Demo.Commands.ResetDemoDatabase;
using StampService.Contracts.DTOs.Audit;
using StampService.Contracts.DTOs.Brands;
using StampService.Contracts.DTOs.Demo;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController : ApiControllerBase
{
    [HttpGet("access")]
    public async Task<EndpointResult<bool>> GetAccess(
        [FromServices] IAdminAccessService adminAccessService,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<bool>();

        var isAdmin = await adminAccessService.IsAdminAsync(
            AdminActor.FromUser(userIdResult.Value),
            cancellationToken);

        return Result.Ok(isAdmin);
    }

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

    [HttpGet("audit-logs")]
    public async Task<EndpointResult<BusinessAuditLogsResponse>> GetAuditLogs(
        [FromQuery] DateTime? occurredFromUtc,
        [FromQuery] DateTime? occurredToUtc,
        [FromQuery] Guid? brandId,
        [FromQuery] string? customerPhoneNumber,
        [FromQuery] string? actorName,
        [FromQuery] string? operationType,
        [FromQuery] string? operationStatus,
        [FromQuery] int? take,
        [FromServices] IQueryHandler<BusinessAuditLogsResponse, GetBusinessAuditLogsQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<BusinessAuditLogsResponse>();

        return await handler.Handle(
            new GetBusinessAuditLogsQuery(
                AdminActor.FromUser(userIdResult.Value),
                occurredFromUtc,
                occurredToUtc,
                brandId,
                customerPhoneNumber,
                actorName,
                operationType,
                operationStatus,
                take ?? 50),
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

    [HttpPost("demo/brands")]
    public async Task<EndpointResult<bool>> CreateDemoBrands(
        [FromServices] ICommandHandler<bool, CreateDemoBrandsCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<bool>();

        return await handler.Handle(
            new CreateDemoBrandsCommand(AdminActor.FromUser(userIdResult.Value)),
            cancellationToken);
    }

    [HttpPost("demo/user-data")]
    public async Task<EndpointResult<bool>> CreateUserDemoData(
        CreateUserDemoDataRequest request,
        [FromServices] ICommandHandler<bool, CreateUserDemoDataCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<bool>();

        return await handler.Handle(
            new CreateUserDemoDataCommand(
                AdminActor.FromUser(userIdResult.Value),
                request.PhoneNumber,
                request.BrandId),
            cancellationToken);
    }

    [HttpPost("demo/reset")]
    public async Task<EndpointResult<bool>> ResetDemoDatabase(
        [FromServices] ICommandHandler<bool, ResetDemoDatabaseCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<bool>();

        return await handler.Handle(
            new ResetDemoDatabaseCommand(AdminActor.FromUser(userIdResult.Value)),
            cancellationToken);
    }
}
