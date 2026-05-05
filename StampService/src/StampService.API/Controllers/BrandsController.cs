using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.Application.Abstractions;
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
}
