using FluentResults;

namespace StampService.Application.Users;

public interface ITelegramLinkSessionProtector
{
    string Protect(TelegramLinkSession session);

    Result<TelegramLinkSession> Unprotect(string token);
}

