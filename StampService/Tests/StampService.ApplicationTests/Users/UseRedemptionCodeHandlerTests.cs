using StampService.Application.Users.Commands.UseRedemptionCode;
using StampService.ApplicationTests.Fakes;
using StampService.Application.Errors;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Users;

public class UseRedemptionCodeHandlerTests
{
    [Fact]
    public async Task Handle_WhenCodeIsActive_ShouldMarkCodeAsUsedAndReturnUserId()
    {
        var now = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var code = RedemptionCode.Create(
            userId,
            "123456",
            now.UtcDateTime.AddMinutes(3),
            now.UtcDateTime).Value;
        var repository = new FakeRedemptionCodeRepository();
        repository.Add(code);
        var handler = new UseRedemptionCodeHandler(
            repository,
            new FixedTimeProvider(now));

        var result = await handler.Handle(
            new UseRedemptionCodeCommand("123456"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.NotNull(code.UsedAtUtc);
        Assert.Equal(0, repository.SaveCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12A456")]
    public async Task Handle_WhenCodeFormatIsInvalid_ShouldFail(string code)
    {
        var handler = new UseRedemptionCodeHandler(
            new FakeRedemptionCodeRepository(),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var result = await handler.Handle(
            new UseRedemptionCodeCommand(code),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(result.Errors.Single());
        Assert.Equal("redemption_code.invalid", error.Code);
    }

    [Fact]
    public async Task Handle_WhenCodeIsExpired_ShouldFail()
    {
        var now = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
        var repository = new FakeRedemptionCodeRepository();
        repository.Add(RedemptionCode.Create(
            Guid.NewGuid(),
            "123456",
            now.UtcDateTime.AddMinutes(-1),
            now.UtcDateTime.AddMinutes(-2)).Value);
        var handler = new UseRedemptionCodeHandler(
            repository,
            new FixedTimeProvider(now));

        var result = await handler.Handle(
            new UseRedemptionCodeCommand("123456"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
