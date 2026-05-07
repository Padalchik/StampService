using StampService.Application.Users;

namespace StampService.ApplicationTests.Fakes;

public class FixedRedemptionCodeGenerator : IRedemptionCodeGenerator
{
    private readonly string _code;

    public FixedRedemptionCodeGenerator(string code)
    {
        _code = code;
    }

    public Task<string> GenerateAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        return Task.FromResult(_code);
    }
}
