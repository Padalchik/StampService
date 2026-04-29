using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StampService.Application.Auth;
using StampService.Application.Services;
using StampService.Contracts.DTOs.Auth;
using StampService.Domain.User;

namespace StampService.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ITelegramValidationService _telegramValidationService;

    public AuthService(
        AppDbContext dbContext,
        IConfiguration configuration,
        ITelegramValidationService telegramValidationService)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _telegramValidationService = telegramValidationService;
    }

    public async Task<Result<AuthResponse>> LoginAsync(TelegramLoginRequest request)
    {
        if (!_telegramValidationService.Validate(request))
            return Result.Fail("Invalid Telegram login data");

        var providerKey = request.Id.ToString();
        var userIdentity = await _dbContext.UserIdentities
            .Include(identity => identity.User)
            .FirstOrDefaultAsync(
                identity => identity.Type == IdentityType.Telegram && identity.Key == providerKey);

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
            await _dbContext.SaveChangesAsync();
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(GetExpiresInMinutes());
        var token = GenerateJwtToken(user, expiresAt);

        return Result.Ok(new AuthResponse(token, user.Id, expiresAt));
    }

    private string GenerateJwtToken(User user, DateTime expiresAt)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Name)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetExpiresInMinutes()
    {
        var value = _configuration["Jwt:ExpiresInMinutes"];

        return int.TryParse(value, out var expiresInMinutes) ? expiresInMinutes : 1440;
    }

    private static string GetDisplayName(TelegramLoginRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Username))
            return request.Username.Trim();

        return $"{request.FirstName} {request.LastName}".Trim();
    }
}
