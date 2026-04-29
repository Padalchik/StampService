using Microsoft.AspNetCore.Mvc;
using StampService.Application.Auth;
using StampService.Contracts.DTOs.Auth;

namespace StampService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("telegram")]
    public async Task<ActionResult<AuthResponse>> Login(
        TelegramLoginRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}
