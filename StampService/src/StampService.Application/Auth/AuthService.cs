using System.Text.Json;
using FluentResults;
using StampService.Application.Errors;
using StampService.Application.Services;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Auth;
using StampService.Domain.User;

namespace StampService.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITelegramValidationService _telegramValidationService;
    private readonly ICustomerCodeGenerator _customerCodeGenerator;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ITelegramValidationService telegramValidationService,
        ICustomerCodeGenerator customerCodeGenerator)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _telegramValidationService = telegramValidationService;
        _customerCodeGenerator = customerCodeGenerator;
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        TelegramLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!_telegramValidationService.Validate(request))
            return Result.Fail(AuthErrors.TelegramLoginDataInvalid());

        var providerKey = request.Id.ToString();
        var user = await _userRepository.GetByIdentityAsync(
            IdentityType.Telegram,
            providerKey,
            cancellationToken);

        if (user is null)
        {
            var customerCode = await _customerCodeGenerator.GenerateAsync(cancellationToken);
            var userResult = User.Create(GetDisplayName(request), customerCode);
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

            _userRepository.Add(user);
            await _userRepository.SaveAsync(cancellationToken);
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
