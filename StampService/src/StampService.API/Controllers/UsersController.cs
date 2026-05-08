using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Errors;
using StampService.Application.Users.Commands.CreateRedemptionCode;
using StampService.Contracts.DTOs.Users;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    [HttpPost("me/redemption-code")]
    public async Task<EndpointResult<CreateRedemptionCodeResponse>> CreateRedemptionCode(
        [FromServices] ICommandHandler<CreateRedemptionCodeResponse, CreateRedemptionCodeCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CreateRedemptionCodeResponse>();

        var command = new CreateRedemptionCodeCommand(userIdResult.Value);

        return await handler.Handle(command, cancellationToken);
    }

    private Result<Guid> GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdValue))
            return Result.Fail(AuthErrors.UserIdClaimMissing());

        return Guid.TryParse(userIdValue, out var userId)
            ? Result.Ok(userId)
            : Result.Fail(AuthErrors.UserIdClaimInvalid());
    }
}
