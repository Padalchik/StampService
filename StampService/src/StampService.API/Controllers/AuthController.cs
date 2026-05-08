using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Auth;
using StampService.Contracts.DTOs.Auth;

namespace StampService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("telegram")]
    public async Task<EndpointResult<AuthResponse>> Login(
        TelegramLoginRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        return await authService.LoginAsync(request, cancellationToken);
    }
}
