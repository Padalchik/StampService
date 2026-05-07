namespace StampService.Application.Users;

public interface IRedemptionCodeGenerator
{
    Task<string> GenerateAsync(DateTime nowUtc, CancellationToken cancellationToken);
}
