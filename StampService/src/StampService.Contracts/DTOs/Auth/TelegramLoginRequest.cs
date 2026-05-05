namespace StampService.Contracts.DTOs.Auth;

public record TelegramLoginRequest(
    long Id,
    string FirstName,
    string? LastName,
    string Username,
    string Hash,
    long AuthDate);
