namespace StampService.Contracts.DTOs.Auth;

public record AuthResponse(string Token, Guid UserId, DateTime ExpiresAt);
