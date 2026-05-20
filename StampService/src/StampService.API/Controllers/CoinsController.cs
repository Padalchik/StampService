using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Coins.Commands.IssueCoins;
using StampService.Application.Coins.Commands.RedeemCoins;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Coins;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class CoinsController : ApiControllerBase
{
    [HttpPost("brands/{brandId:guid}/coins/issue")]
    public async Task<EndpointResult<CoinOperationResponse>> Issue(
        Guid brandId,
        IssueCoinsRequest request,
        [FromServices] ICommandHandler<CoinOperationResponse, IssueCoinsCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CoinOperationResponse>();

        return await handler.Handle(
            new IssueCoinsCommand(
                brandId,
                userIdResult.Value,
                request.CustomerCode,
                request.Amount,
                string.IsNullOrWhiteSpace(request.Comment) ? "Issue coins" : request.Comment.Trim()),
            cancellationToken);
    }

    [HttpPost("brands/{brandId:guid}/coins/issue-by-phone")]
    public async Task<EndpointResult<CoinOperationResponse>> IssueByPhone(
        Guid brandId,
        IssueCoinsByPhoneRequest request,
        [FromServices] IRecipientResolver recipientResolver,
        [FromServices] ICommandHandler<CoinOperationResponse, IssueCoinsCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CoinOperationResponse>();

        var recipientResult = await recipientResolver.ResolveByPhoneAsync(
            request.PhoneNumber,
            cancellationToken);

        if (recipientResult.IsFailed)
            return recipientResult.ToResult<CoinOperationResponse>();

        return await handler.Handle(
            new IssueCoinsCommand(
                brandId,
                userIdResult.Value,
                recipientResult.Value.PublicIdentifier,
                request.Amount,
                string.IsNullOrWhiteSpace(request.Comment) ? "Issue coins" : request.Comment.Trim()),
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
