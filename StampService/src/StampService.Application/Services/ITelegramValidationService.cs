using StampService.Contracts.DTOs.Auth;

namespace StampService.Application.Services;

public interface ITelegramValidationService
{
    bool Validate(TelegramLoginRequest request);
}
