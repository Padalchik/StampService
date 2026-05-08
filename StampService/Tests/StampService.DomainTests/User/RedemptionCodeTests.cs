using StampService.Domain.Shared;
using StampService.Domain.User;

namespace StampService.DomainTests.User;

public class RedemptionCodeTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCode()
    {
        var now = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc);

        var result = RedemptionCode.Create(
            Guid.NewGuid(),
            "123456",
            now.AddMinutes(3),
            now);

        Assert.True(result.IsSuccess);
        Assert.Equal("123456", result.Value.Code);
        Assert.True(result.Value.IsActive(now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12A456")]
    public void Create_WithInvalidCode_ShouldFail(string code)
    {
        var now = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc);

        var result = RedemptionCode.Create(
            Guid.NewGuid(),
            code,
            now.AddMinutes(3),
            now);

        var error = Assert.IsType<DomainError>(result.Errors.Single());
        Assert.Equal("redemption_code.code_invalid", error.Code);
    }

    [Fact]
    public void Use_WhenCodeIsActive_ShouldMarkAsUsed()
    {
        var now = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc);
        var code = RedemptionCode.Create(Guid.NewGuid(), "123456", now.AddMinutes(3), now).Value;

        var result = code.Use(now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.NotNull(code.UsedAtUtc);
        Assert.False(code.IsActive(now.AddMinutes(1)));
    }

    [Fact]
    public void Use_WhenCodeIsExpired_ShouldFail()
    {
        var now = new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc);
        var code = RedemptionCode.Create(Guid.NewGuid(), "123456", now.AddMinutes(3), now).Value;

        var result = code.Use(now.AddMinutes(3));

        var error = Assert.IsType<DomainError>(result.Errors.Single());
        Assert.Equal("redemption_code.expired", error.Code);
    }
}
