using System.Text.Json;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using StampService.Application.Auth;
using StampService.Application.Services;
using StampService.Contracts.DTOs.Auth;
using StampService.Domain.User;

namespace StampService.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITelegramValidationService _telegramValidationService;

    public AuthService(
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService,
        ITelegramValidationService telegramValidationService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _telegramValidationService = telegramValidationService;
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        TelegramLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!_telegramValidationService.Validate(request))
            return Result.Fail("Invalid Telegram login data");

        var providerKey = request.Id.ToString();
        var userIdentity = await _dbContext.UserIdentities
            .Include(identity => identity.User)
            .FirstOrDefaultAsync(
                identity => identity.Type == IdentityType.Telegram && identity.Key == providerKey,
                cancellationToken);

        var user = userIdentity?.User;
        if (user is null)
        {
            var userResult = User.Create(GetDisplayName(request));
            if (userResult.IsFailed)
                return Result.Fail(userResult.Errors);

            user = userResult.Value;

            var metadata = JsonSerializer.Serialize(new
            {
                request.Id,
                request.FirstName,
                request.LastName,
                request.Username,
                request.AuthDate
            });

            var identityResult = user.AddIdentity(IdentityType.Telegram, providerKey, metadata);
            if (identityResult.IsFailed)
                return Result.Fail(identityResult.Errors);

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var token = _jwtTokenService.CreateToken(user);

        return Result.Ok(new AuthResponse(token.Value, user.Id, token.ExpiresAt));
    }

    private static string GetDisplayName(TelegramLoginRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Username))
            return request.Username.Trim();

        return $"{request.FirstName} {request.LastName}".Trim();
    }
}
