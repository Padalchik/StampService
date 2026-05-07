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
}
