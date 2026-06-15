using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.Users.Commands.ConfirmPhoneChangeCode;
using StampService.Application.Users.Commands.ConfirmPhoneLinkCode;
using StampService.Application.Users.Commands.ConfirmTelegramLink;
using StampService.Application.Users.Commands.CreateRedemptionCode;
using StampService.Application.Users.Commands.RequestPhoneChangeCode;
using StampService.Application.Users.Commands.RequestPhoneLinkCode;
using StampService.Application.Users.Commands.RequestTelegramLink;
using StampService.Application.Users.Queries.GetMyProfile;
using StampService.Contracts.DTOs.Auth;
using StampService.Contracts.DTOs.Profile;
using StampService.Contracts.DTOs.Users;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ApiControllerBase
{
    [HttpGet("me")]
    public async Task<EndpointResult<MyProfileResponse>> GetMe(
        [FromServices] IQueryHandler<MyProfileResponse, GetMyProfileQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<MyProfileResponse>();

        return await handler.Handle(new GetMyProfileQuery(userIdResult.Value), cancellationToken);
    }

    [HttpPost("me/redemption-code")]
    public async Task<EndpointResult<CreateRedemptionCodeResponse>> CreateRedemptionCode(
        [FromQuery] bool forceRefreshCode,
        [FromServices] ICommandHandler<CreateRedemptionCodeResponse, CreateRedemptionCodeCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CreateRedemptionCodeResponse>();

        var command = new CreateRedemptionCodeCommand(userIdResult.Value, forceRefreshCode);

        return await handler.Handle(command, cancellationToken);
    }

    [HttpPost("me/phone/code")]
    public async Task<EndpointResult<RequestPhoneLinkCodeResponse>> RequestPhoneLinkCode(
        RequestPhoneLinkCodeRequest request,
        [FromServices] ICommandHandler<RequestPhoneLinkCodeResponse, RequestPhoneLinkCodeCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<RequestPhoneLinkCodeResponse>();

        return await handler.Handle(
            new RequestPhoneLinkCodeCommand(userIdResult.Value, request.PhoneNumber),
            cancellationToken);
    }

    [HttpPost("me/phone/verify")]
    public async Task<EndpointResult<ConfirmPhoneLinkCodeResponse>> ConfirmPhoneLinkCode(
        ConfirmPhoneLinkCodeRequest request,
        [FromServices] ICommandHandler<ConfirmPhoneLinkCodeResponse, ConfirmPhoneLinkCodeCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<ConfirmPhoneLinkCodeResponse>();

        return await handler.Handle(
            new ConfirmPhoneLinkCodeCommand(
                userIdResult.Value,
                request.PhoneNumber,
                request.Code,
                request.AuthCodeId),
            cancellationToken);
    }

    [HttpPost("me/phone/change/code")]
    public async Task<EndpointResult<RequestPhoneLinkCodeResponse>> RequestPhoneChangeCode(
        RequestPhoneLinkCodeRequest request,
        [FromServices] ICommandHandler<RequestPhoneLinkCodeResponse, RequestPhoneChangeCodeCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<RequestPhoneLinkCodeResponse>();

        return await handler.Handle(
            new RequestPhoneChangeCodeCommand(userIdResult.Value, request.PhoneNumber),
            cancellationToken);
    }

    [HttpPost("me/phone/change/verify")]
    public async Task<EndpointResult<ConfirmPhoneLinkCodeResponse>> ConfirmPhoneChangeCode(
        ConfirmPhoneLinkCodeRequest request,
        [FromServices] ICommandHandler<ConfirmPhoneLinkCodeResponse, ConfirmPhoneChangeCodeCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<ConfirmPhoneLinkCodeResponse>();

        return await handler.Handle(
            new ConfirmPhoneChangeCodeCommand(
                userIdResult.Value,
                request.PhoneNumber,
                request.Code,
                request.AuthCodeId),
            cancellationToken);
    }

    [HttpPost("me/telegram")]
    public async Task<EndpointResult<ConfirmTelegramLinkResponse>> ConfirmTelegramLink(
        TelegramLoginRequest request,
        [FromServices] ICommandHandler<ConfirmTelegramLinkResponse, ConfirmTelegramLinkCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<ConfirmTelegramLinkResponse>();

        return await handler.Handle(
            new ConfirmTelegramLinkCommand(userIdResult.Value, request),
            cancellationToken);
    }

    [HttpPost("me/telegram/link")]
    public async Task<EndpointResult<RequestTelegramLinkResponse>> RequestTelegramLink(
        [FromServices] ICommandHandler<RequestTelegramLinkResponse, RequestTelegramLinkCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<RequestTelegramLinkResponse>();

        return await handler.Handle(
            new RequestTelegramLinkCommand(userIdResult.Value),
            cancellationToken);
    }
}
