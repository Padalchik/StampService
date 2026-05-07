namespace StampService.Application.Users;

public record RecipientResolutionResult(
    Guid UserId,
    string DisplayName,
    string PublicIdentifier);
