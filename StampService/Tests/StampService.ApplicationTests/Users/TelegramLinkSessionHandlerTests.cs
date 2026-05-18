using FluentResults;
using Microsoft.Extensions.Options;
using StampService.Application.Services;
using StampService.Application.Users;
using StampService.Application.Users.Commands.ConfirmTelegramLinkSession;
using StampService.Application.Users.Commands.RequestTelegramLink;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Users;

public class TelegramLinkSessionHandlerTests
{
    [Fact]
    public async Task ConfirmTelegramLinkSession_WhenTokenIsValid_ShouldAddTelegramIdentityToWebUser()
    {
        var fixture = CreateFixture();
        var user = User.Create("phone-user").Value;
        user.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        fixture.Users.Add(user);

        var requestResult = await fixture.RequestHandler.Handle(
            new RequestTelegramLinkCommand(user.Id),
            CancellationToken.None);
        var token = new Uri(requestResult.Value.TelegramLinkUrl).Query.Split("start=")[1];
        var confirmResult = await fixture.ConfirmHandler.Handle(
            new ConfirmTelegramLinkSessionCommand(
                token,
                278225388,
                "Andrey",
                null,
                "andrey"),
            CancellationToken.None);

        Assert.True(confirmResult.IsSuccess);
        Assert.Contains(user.Identities, identity => identity.Type == IdentityType.Telegram && identity.Key == "278225388");
        Assert.Equal("andrey", confirmResult.Value.DisplayName);
    }

    [Fact]
    public async Task ConfirmTelegramLinkSession_WhenTelegramBelongsToAnotherUser_ShouldFail()
    {
        var fixture = CreateFixture();
        var user = User.Create("phone-user").Value;
        var anotherUser = User.Create("telegram-user").Value;
        anotherUser.AddIdentity(IdentityType.Telegram, "278225388", "{}");
        fixture.Users.Add(user);
        fixture.Users.Add(anotherUser);

        var token = fixture.Protector.Protect(new TelegramLinkSession(
            user.Id,
            new DateTime(2026, 5, 18, 10, 10, 0, DateTimeKind.Utc)));
        var result = await fixture.ConfirmHandler.Handle(
            new ConfirmTelegramLinkSessionCommand(token, 278225388, null, null, "andrey"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    private static Fixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var protector = new FakeTelegramLinkSessionProtector();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero));
        var telegramOptions = Options.Create(new TelegramOptions { BotUsername = "StampServiceBot" });

        return new Fixture(
            users,
            protector,
            new RequestTelegramLinkHandler(users, protector, timeProvider, telegramOptions),
            new ConfirmTelegramLinkSessionHandler(users, protector, timeProvider));
    }

    private sealed record Fixture(
        FakeUserRepository Users,
        FakeTelegramLinkSessionProtector Protector,
        RequestTelegramLinkHandler RequestHandler,
        ConfirmTelegramLinkSessionHandler ConfirmHandler);

    private sealed class FakeTelegramLinkSessionProtector : ITelegramLinkSessionProtector
    {
        private readonly Dictionary<string, TelegramLinkSession> _sessions = [];
        private int _nextId;

        public string Protect(TelegramLinkSession session)
        {
            var token = $"l_test{++_nextId}";
            _sessions[token] = session;
            return token;
        }

        public Result<TelegramLinkSession> Unprotect(string token)
        {
            return _sessions.TryGetValue(token, out var session)
                ? Result.Ok(session)
                : Result.Fail<TelegramLinkSession>("Invalid token");
        }
    }
}
