using Microsoft.AspNetCore.Mvc;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Commands.CreateBrand;
using StampService.Contracts.DTOs.Brands;

namespace StampService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateBrandResponse>> Create(
        CreateBrandRequest request,
        [FromServices] ICommandHandler<CreateBrandResponse, CreateBrandCommand> handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.NewGuid();
        var command = new CreateBrandCommand(request, userId);

        var result = await handler.Handle(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}
