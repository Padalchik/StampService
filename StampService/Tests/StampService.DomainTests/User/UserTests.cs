using StampService.Domain.Shared;
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
    public void Create_WithInvalidCustomerCode_ShouldReturnTypedDomainError()
    {
        var result = DomainUser.Create("Ivan", "12A4");

        var error = Assert.IsType<DomainError>(result.Errors.Single());
        Assert.Equal("user.customer_code_invalid", error.Code);
        Assert.Equal(DomainErrorType.Validation, error.Type);
        Assert.Equal("customerCode", error.InvalidField);
    }

    [Fact]
    public void Create_WithoutExplicitCustomerCode_ShouldGenerateFourDigitCode()
    {
        var result = DomainUser.Create("Ivan");

        Assert.True(result.IsSuccess);
        Assert.Matches("^[0-9]{4}$", result.Value.CustomerCode);
    }

    [Fact]
    public void AddIdentity_WithInvalidPhoneKey_ShouldFail()
    {
        var user = DomainUser.Create("Ivan").Value;

        var result = user.AddIdentity(
            StampService.Domain.User.IdentityType.Phone,
            "79991234567",
            "{}");

        Assert.True(result.IsFailed);
        var error = Assert.IsType<DomainError>(result.Errors.Single());
        Assert.Equal("user_identity.phone_key_invalid", error.Code);
    }
}
