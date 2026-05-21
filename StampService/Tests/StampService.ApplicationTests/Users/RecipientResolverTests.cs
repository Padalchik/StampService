using StampService.Application.Users;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Users;

public class RecipientResolverTests
{
    [Fact]
    public async Task ResolveAsync_WhenCustomerCodeExists_ShouldReturnRecipient()
    {
        var repository = new FakeUserRepository();
        var user = User.Create("Ivan", "1234").Value;
        repository.Add(user);
        var resolver = new RecipientResolver(repository);

        var result = await resolver.ResolveAsync("1234", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.Equal("Ivan", result.Value.DisplayName);
        Assert.Equal("1234", result.Value.PublicIdentifier);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12A4")]
    public async Task ResolveAsync_WhenCustomerCodeIsInvalid_ShouldFail(string customerCode)
    {
        var resolver = new RecipientResolver(new FakeUserRepository());

        var result = await resolver.ResolveAsync(customerCode, CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task ResolveAsync_WhenCustomerCodeDoesNotExist_ShouldFail()
    {
        var resolver = new RecipientResolver(new FakeUserRepository());

        var result = await resolver.ResolveAsync("1234", CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task ResolveByPhoneAsync_WhenPhoneIdentityExists_ShouldReturnRecipient()
    {
        var repository = new FakeUserRepository();
        var user = User.Create("Ivan", "1234").Value;
        user.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        repository.Add(user);
        var resolver = new RecipientResolver(repository);

        var result = await resolver.ResolveByPhoneAsync("+7 999 123-45-67", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.Equal("Ivan", result.Value.DisplayName);
        Assert.Equal("1234", result.Value.PublicIdentifier);
    }

    [Theory]
    [InlineData("")]
    [InlineData("79991234567")]
    [InlineData("+7 abc")]
    public async Task ResolveByPhoneAsync_WhenPhoneIsInvalid_ShouldFail(string phoneNumber)
    {
        var resolver = new RecipientResolver(new FakeUserRepository());

        var result = await resolver.ResolveByPhoneAsync(phoneNumber, CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task ResolveByPhoneAsync_WhenPhoneIdentityDoesNotExist_ShouldFail()
    {
        var resolver = new RecipientResolver(new FakeUserRepository());

        var result = await resolver.ResolveByPhoneAsync("+79991234567", CancellationToken.None);

        Assert.True(result.IsFailed);
    }
}
