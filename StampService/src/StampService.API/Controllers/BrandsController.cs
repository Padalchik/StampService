using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Commands.AddBrandStaff;
using StampService.Application.Brands.Commands.AssignBrandOwner;
using StampService.Application.Brands.Commands.CreateBrand;
using StampService.Contracts.DTOs.Brands;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateBrandResponse>> Create(
        CreateBrandRequest request,
        [FromServices] ICommandHandler<CreateBrandResponse, CreateBrandCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateBrandCommand(request);

        var result = await handler.Handle(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPost("{brandId:guid}/owner")]
    public async Task<ActionResult<AssignBrandOwnerResponse>> AssignOwner(
        Guid brandId,
        AssignBrandOwnerRequest request,
        [FromServices] ICommandHandler<AssignBrandOwnerResponse, AssignBrandOwnerCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new AssignBrandOwnerCommand(brandId, request);

        var result = await handler.Handle(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }

    [HttpPost("{brandId:guid}/staff")]
    public async Task<ActionResult<AddBrandStaffResponse>> AddStaff(
        Guid brandId,
        AddBrandStaffRequest request,
        [FromServices] ICommandHandler<AddBrandStaffResponse, AddBrandStaffCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return Unauthorized(userIdResult.Errors);

        var command = new AddBrandStaffCommand(brandId, userIdResult.Value, request);

        var result = await handler.Handle(command, cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.Errors.Any(error => error.Message == "Access denied"))
            return Forbid();

        return BadRequest(result.Errors);
    }

    private Result<Guid> GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdValue))
            return Result.Fail("User id claim is missing");

        return Guid.TryParse(userIdValue, out var userId)
            ? Result.Ok(userId)
            : Result.Fail("User id claim is invalid");
    }
}
