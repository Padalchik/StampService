using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Wallet.Commands.OpenUserWallet;
using StampService.Contracts.DTOs.Wallet;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/wallet")]
public class WalletController : ApiControllerBase
{
    [HttpPost("open")]
    public async Task<EndpointResult<UserWalletResponse>> Open(
        [FromQuery] bool forceRefreshCode,
        [FromServices] ICommandHandler<UserWalletResponse, OpenUserWalletCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<UserWalletResponse>();

        return await handler.Handle(
            new OpenUserWalletCommand(userIdResult.Value, forceRefreshCode),
            cancellationToken);
    }
}
