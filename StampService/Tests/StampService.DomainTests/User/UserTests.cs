using StampService.Domain.Shared;
using DomainUser = StampService.Domain.User.User;

namespace StampService.DomainTests.User;

public class UserTests
{
    [Fact]
    public void Create_WithValidName_ShouldCreateUser()
    {
        var result = DomainUser.Create("Ivan");

        Assert.True(result.IsSuccess);
        Assert.Equal("Ivan", result.Value.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldFail(string name)
    {
        var result = DomainUser.Create(name);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Create_WithInvalidName_ShouldReturnTypedDomainError()
    {
        var result = DomainUser.Create(" ");

        var error = Assert.IsType<DomainError>(result.Errors.Single());
        Assert.Equal("user.name_required", error.Code);
        Assert.Equal(DomainErrorType.Validation, error.Type);
        Assert.Equal("name", error.InvalidField);
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

    [Fact]
    public void HasActiveIdentity_WhenIdentityIsActive_ShouldReturnTrue()
    {
        var user = DomainUser.Create("Ivan").Value;
        user.AddIdentity(
            StampService.Domain.User.IdentityType.Phone,
            "+79991234567",
            "{}");

        Assert.True(user.HasActiveIdentity(StampService.Domain.User.IdentityType.Phone));
    }

}
