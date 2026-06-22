using System.Text.Json;
using StampService.Application.Users;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Users;

public class PhoneAccountServiceTests
{
    [Fact]
    public async Task GetOrCreateForBusinessOperationAsync_WhenPhoneIdentityExists_ShouldReturnExistingUser()
    {
        var repository = new FakeUserRepository();
        var existingUser = User.Create("Existing customer").Value;
        existingUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        repository.Add(existingUser);
        var service = CreateService(repository);

        var result = await service.GetOrCreateForBusinessOperationAsync(
            "+7 999 123-45-67",
            "phoneNumber",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingUser.Id, result.Value.Id);
        Assert.Single(repository.Users);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task GetOrCreateForBusinessOperationAsync_WhenPhoneIdentityWasDeactivated_ShouldNotReturnOldUser()
    {
        var repository = new FakeUserRepository();
        var existingUser = User.Create("Existing customer").Value;
        var identity = existingUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}").Value;
        identity.Deactivate(new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc));
        repository.Add(existingUser);
        var service = CreateService(repository);

        var result = await service.GetOrCreateForBusinessOperationAsync(
            "+7 999 123-45-67",
            "phoneNumber",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(existingUser.Id, result.Value.Id);
        Assert.Equal(2, repository.Users.Count);
        Assert.Contains(repository.Users, user => user.Id == existingUser.Id);
        Assert.Contains(repository.Users, user => user.Id == result.Value.Id);
    }

    [Fact]
    public async Task GetOrCreateForBusinessOperationAsync_WhenPhoneIdentityDoesNotExist_ShouldCreatePhoneUser()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var result = await service.GetOrCreateForBusinessOperationAsync(
            "+7 999 123-45-67",
            "phoneNumber",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = Assert.Single(repository.Users);
        Assert.Equal("Business customer", user.Name);
        Assert.Equal(user.Id, result.Value.Id);
        var phoneIdentity = Assert.Single(
            user.Identities,
            identity => identity.Type == IdentityType.Phone
                && identity.Key == "+79991234567");
        using var metadata = JsonDocument.Parse(phoneIdentity.Metadata);
        Assert.Equal(
            "+79991234567",
            metadata.RootElement.GetProperty("PhoneNumber").GetString());
        Assert.Equal(0, repository.SaveCount);
    }

    [Theory]
    [InlineData("79991234567")]
    [InlineData("+7abc9991234567")]
    [InlineData("++79991234567")]
    public async Task GetOrCreateForBusinessOperationAsync_WhenPhoneIsInvalid_ShouldFail(string phoneNumber)
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var result = await service.GetOrCreateForBusinessOperationAsync(
            phoneNumber,
            "phoneNumber",
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(repository.Users);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task GetExistingForBusinessOperationAsync_WhenPhoneIdentityExists_ShouldReturnExistingUser()
    {
        var repository = new FakeUserRepository();
        var existingUser = User.Create("Existing customer").Value;
        existingUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        repository.Add(existingUser);
        var service = CreateService(repository);

        var result = await service.GetExistingForBusinessOperationAsync(
            "+7 999 123-45-67",
            "phoneNumber",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingUser.Id, result.Value.Id);
        Assert.Single(repository.Users);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task GetExistingForBusinessOperationAsync_WhenPhoneIdentityDoesNotExist_ShouldFailWithoutCreatingUser()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var result = await service.GetExistingForBusinessOperationAsync(
            "+7 999 123-45-67",
            "phoneNumber",
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(repository.Users);
        Assert.Equal(0, repository.SaveCount);
    }

    private static PhoneAccountService CreateService(FakeUserRepository repository)
    {
        return new PhoneAccountService(
            repository,
            new FixedDisplayNameGenerator());
    }

    private sealed class FixedDisplayNameGenerator : IUserDisplayNameGenerator
    {
        public string Generate() => "Business customer";
    }
}
