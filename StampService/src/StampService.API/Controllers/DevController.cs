using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FluentResults;
using StampService.API.EndpointResults;
using StampService.Application.Errors;
using StampService.Application.Services;
using StampService.Contracts.DTOs.Auth;

namespace StampService.API.Controllers;

[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly TelegramOptions _telegramOptions;

    public DevController(
        IWebHostEnvironment environment,
        IOptions<TelegramOptions> telegramOptions)
    {
        _environment = environment;
        _telegramOptions = telegramOptions.Value;
    }

    [HttpPost("telegram-auth-payload")]
    public EndpointResult<TelegramLoginRequest> CreateTelegramAuthPayload(
        CreateTelegramAuthPayloadRequest request)
    {
        if (!_environment.IsDevelopment())
            return Result.Fail<TelegramLoginRequest>(GeneralErrors.NotFound());

        if (string.IsNullOrWhiteSpace(_telegramOptions.BotToken))
            return Result.Fail<TelegramLoginRequest>(
                GeneralErrors.Failure("Telegram:BotToken is not configured"));

        var authRequest = new TelegramLoginRequest(
            request.Id,
            request.FirstName,
            request.LastName,
            request.Username,
            Hash: string.Empty,
            AuthDate: DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        var hash = TelegramAuthHashCalculator.ComputeHash(
            authRequest,
            _telegramOptions.BotToken);

        return Result.Ok(authRequest with { Hash = hash });
    }
}

public record CreateTelegramAuthPayloadRequest(
    long Id,
    string FirstName,
    string? LastName,
    string? Username);
