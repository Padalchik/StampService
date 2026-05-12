using StampService.Application.Users;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Users;

public class RedemptionCodeGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ShouldReturnFourDigitCode()
    {
        var generator = new RedemptionCodeGenerator(new FakeRedemptionCodeRepository());

        var code = await generator.GenerateAsync(
            new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.True(RedemptionCode.IsValidCode(code));
        Assert.Equal(4, code.Length);
    }

    [Fact]
    public async Task GenerateAsync_WhenOnlyOneCombinationIsFree_ShouldReturnFreeCode()
    {
        var now = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc);
        var reservedCodes = new HashSet<string>();
        for (var value = 0; value < 9_999; value++)
        {
            reservedCodes.Add(value.ToString("D4"));
        }

        var repository = new ReservedCodeRepository(reservedCodes);
        var generator = new RedemptionCodeGenerator(repository);

        var code = await generator.GenerateAsync(now, CancellationToken.None);

        Assert.Equal("9999", code);
    }

    [Fact]
    public async Task GenerateAsync_WhenAllCombinationsAreReserved_ShouldThrow()
    {
        var now = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc);
        var reservedCodes = new HashSet<string>();
        for (var value = 0; value < 10_000; value++)
        {
            reservedCodes.Add(value.ToString("D4"));
        }

        var repository = new ReservedCodeRepository(reservedCodes);
        var generator = new RedemptionCodeGenerator(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            generator.GenerateAsync(now, CancellationToken.None));
    }

    private sealed class ReservedCodeRepository : IRedemptionCodeRepository
    {
        private readonly HashSet<string> _reservedCodes;

        public ReservedCodeRepository(HashSet<string> reservedCodes)
        {
            _reservedCodes = reservedCodes;
        }

        public Task<RedemptionCode?> GetActiveByUserIdAsync(
            Guid userId,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<RedemptionCode?> GetActiveByCodeAsync(
            string code,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ActiveCodeExistsAsync(
            string code,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_reservedCodes.Contains(code));
        }

        public void Add(RedemptionCode redemptionCode)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
