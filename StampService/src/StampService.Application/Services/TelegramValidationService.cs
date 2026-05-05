using StampService.Contracts.DTOs.Auth;

namespace StampService.Application.Services;

public class TelegramValidationService : ITelegramValidationService
{
    public bool Validate(TelegramLoginRequest request)
    {
        // MVP: настоящую проверку Telegram hash добавим после подключения bot token.
        return !string.IsNullOrWhiteSpace(request.Hash);
    }
}
