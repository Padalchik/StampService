using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.CreateRedemptionCode;
using StampService.Contracts.DTOs.Users;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ApiControllerBase
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

}
