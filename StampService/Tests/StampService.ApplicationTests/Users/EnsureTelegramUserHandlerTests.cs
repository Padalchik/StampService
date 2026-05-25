using StampService.Application.Users;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Users;

public class EnsureTelegramUserHandlerTests
{
    [Fact]
    public async Task Handle_WhenTelegramUserDoesNotExist_ShouldFail()
    {
        var repository = new FakeUserRepository();
        var handler = new EnsureTelegramUserHandler(
            repository,
            CreatePhoneAccountService(repository));

        var result = await handler.Handle(
            new EnsureTelegramUserCommand(
                123456,
                "Ivan",
                "Petrov",
                "ivan"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(repository.Users);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenTelegramUserExists_ShouldReturnExistingUser()
    {
        var repository = new FakeUserRepository();
        var existingUser = User.Create("existing").Value;
        existingUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        existingUser.AddIdentity(IdentityType.Telegram, "123456", "{}");
        repository.Add(existingUser);
        var handler = new EnsureTelegramUserHandler(
            repository,
            CreatePhoneAccountService(repository));

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
        Assert.Equal(existingUser.Name, result.Value.DisplayName);
        Assert.Single(repository.Users);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenTelegramUserIdIsInvalid_ShouldFail()
    {
        var repository = new FakeUserRepository();
        var handler = new EnsureTelegramUserHandler(
            repository,
            CreatePhoneAccountService(repository));

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
    public async Task Handle_WhenTelegramUserHasNoPhoneIdentity_ShouldFail()
    {
        var repository = new FakeUserRepository();
        var existingUser = User.Create("existing").Value;
        existingUser.AddIdentity(IdentityType.Telegram, "123456", "{}");
        repository.Add(existingUser);
        var handler = new EnsureTelegramUserHandler(
            repository,
            CreatePhoneAccountService(repository));

        var result = await handler.Handle(
            new EnsureTelegramUserCommand(
                123456,
                "Ivan",
                "Petrov",
                "ivan"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    private static PhoneAccountService CreatePhoneAccountService(FakeUserRepository repository)
    {
        return new PhoneAccountService(
            repository,
            new CustomerCodeGenerator(repository),
            new CuteUserDisplayNameGenerator());
    }
}
