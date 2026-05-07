using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
    public ActionResult<TelegramLoginRequest> CreateTelegramAuthPayload(
        CreateTelegramAuthPayloadRequest request)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(_telegramOptions.BotToken))
            return BadRequest("Telegram:BotToken is not configured");

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

        return Ok(authRequest with { Hash = hash });
    }
}

public record CreateTelegramAuthPayloadRequest(
    long Id,
    string FirstName,
    string? LastName,
    string? Username);
