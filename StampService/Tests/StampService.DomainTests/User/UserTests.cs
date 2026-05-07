using DomainUser = StampService.Domain.User.User;

namespace StampService.DomainTests.User;

public class UserTests
{
    [Fact]
    public void Create_WithValidCustomerCode_ShouldCreateUser()
    {
        var result = DomainUser.Create("Ivan", "1234");

        Assert.True(result.IsSuccess);
        Assert.Equal("Ivan", result.Value.Name);
        Assert.Equal("1234", result.Value.CustomerCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12A4")]
    public void Create_WithInvalidCustomerCode_ShouldFail(string customerCode)
    {
        var result = DomainUser.Create("Ivan", customerCode);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Create_WithoutExplicitCustomerCode_ShouldGenerateFourDigitCode()
    {
        var result = DomainUser.Create("Ivan");

        Assert.True(result.IsSuccess);
        Assert.Matches("^[0-9]{4}$", result.Value.CustomerCode);
    }
}
