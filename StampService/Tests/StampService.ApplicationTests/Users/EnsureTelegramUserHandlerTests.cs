using StampService.Application.Users;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Users;

public class EnsureTelegramUserHandlerTests
{
    [Fact]
    public async Task Handle_WhenTelegramUserDoesNotExist_ShouldCreateUserWithTelegramIdentity()
    {
        var repository = new FakeUserRepository();
        var handler = new EnsureTelegramUserHandler(
            repository,
            new CustomerCodeGenerator(repository));

        var result = await handler.Handle(
            new EnsureTelegramUserCommand(
                123456,
                "Ivan",
                "Petrov",
                "ivan"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);
        Assert.Equal("ivan", result.Value.DisplayName);
        Assert.Matches("^[0-9]{4}$", result.Value.CustomerCode);
        Assert.Single(repository.Users);
        Assert.Equal(1, repository.SaveCount);
        Assert.Contains(
            repository.Users[0].Identities,
            identity => identity.Type == IdentityType.Telegram && identity.Key == "123456");
    }

    [Fact]
    public async Task Handle_WhenTelegramUserExists_ShouldReturnExistingUser()
    {
        var repository = new FakeUserRepository();
        var existingUser = User.Create("existing").Value;
        existingUser.AddIdentity(IdentityType.Telegram, "123456", "{}");
        repository.Add(existingUser);
        var handler = new EnsureTelegramUserHandler(
            repository,
            new CustomerCodeGenerator(repository));

        var result = await handler.Handle(
            new EnsureTelegramUserCommand(
                123456,
                "Ivan",
                "Petrov",
                "ivan"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Created);
        Assert.Equal(existingUser.Id, result.Value.UserId);
        Assert.Equal(existingUser.CustomerCode, result.Value.CustomerCode);
        Assert.Single(repository.Users);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenTelegramUserIdIsInvalid_ShouldFail()
    {
        var repository = new FakeUserRepository();
        var handler = new EnsureTelegramUserHandler(
            repository,
            new CustomerCodeGenerator(repository));

        var result = await handler.Handle(
            new EnsureTelegramUserCommand(
                0,
                "Ivan",
                "Petrov",
                "ivan"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenTelegramUserHasNoNames_ShouldUseTelegramIdAsDisplayName()
    {
        var repository = new FakeUserRepository();
        var handler = new EnsureTelegramUserHandler(
            repository,
            new CustomerCodeGenerator(repository));

        var result = await handler.Handle(
            new EnsureTelegramUserCommand(
                123456,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("123456", result.Value.DisplayName);
        Assert.Equal("123456", repository.Users.Single().Name);
    }
}
