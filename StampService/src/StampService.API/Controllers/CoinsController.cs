using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Coins.Commands.IssueCoins;
using StampService.Application.Coins.Commands.RedeemCoins;
using StampService.Contracts.DTOs.Coins;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class CoinsController : ApiControllerBase
{
    [HttpPost("brands/{brandId:guid}/coins/issue-by-phone")]
    public async Task<EndpointResult<CoinOperationResponse>> IssueByPhone(
        Guid brandId,
        IssueCoinsByPhoneRequest request,
        [FromServices] ICommandHandler<CoinOperationResponse, IssueCoinsByPhoneCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CoinOperationResponse>();

        return await handler.Handle(
            new IssueCoinsByPhoneCommand(
                brandId,
                userIdResult.Value,
                request),
            cancellationToken);
    }

    [HttpPost("brands/{brandId:guid}/coins/redeem")]
    public async Task<EndpointResult<CoinOperationResponse>> Redeem(
        Guid brandId,
        RedeemCoinsRequest request,
        [FromServices] ICommandHandler<CoinOperationResponse, RedeemCoinsCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CoinOperationResponse>();

        return await handler.Handle(
            new RedeemCoinsCommand(
                brandId,
                userIdResult.Value,
                request.RedemptionCode,
                request.Amount,
                request.Comment),
            cancellationToken);
    }
}
