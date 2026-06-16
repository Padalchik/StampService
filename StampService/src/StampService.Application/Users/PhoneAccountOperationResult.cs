using StampService.Domain.User;

namespace StampService.Application.Users;

public record PhoneAccountOperationResult(User User, bool Created);
