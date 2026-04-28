using Microsoft.AspNetCore.Mvc;
using StampService.Application.Brand;
using StampService.Contracts.DTOs.Brands;

namespace StampService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;

    public BrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    [HttpPost]
    public async Task<ActionResult<CreateBrandResponse>> Create(CreateBrandRequest request)
    {
        var userId = Guid.NewGuid();

        var result = await _brandService.CreateAsync(request, userId);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}
