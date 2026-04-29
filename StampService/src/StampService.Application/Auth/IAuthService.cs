using FluentResults;
using StampService.Contracts.DTOs.Auth;

namespace StampService.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(TelegramLoginRequest request);
}
