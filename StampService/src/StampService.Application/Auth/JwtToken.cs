namespace StampService.Application.Auth;

public record JwtToken(string Value, DateTime ExpiresAt);
