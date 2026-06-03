using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Users;
using StampService.Application.Users.Commands.ConfirmPhoneChangeCode;
using StampService.Application.Users.Commands.ConfirmPhoneLinkCode;
using StampService.Application.Users.Commands.RequestPhoneChangeCode;
using StampService.Application.Users.Commands.RequestPhoneLinkCode;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Users;

public class PhoneLinkHandlerTests
{
    [Fact]
    public async Task ConfirmPhoneLink_WhenCodeIsValid_ShouldAddPhoneIdentityToUser()
    {
        var fixture = CreateFixture();
        var user = User.Create("telegram-user").Value;
        user.AddIdentity(IdentityType.Telegram, "278225388", "{}");
        fixture.Users.Add(user);
        await fixture.RequestHandler.Handle(
            new RequestPhoneLinkCodeCommand(user.Id, "+79991234567"),
            CancellationToken.None);

        var result = await fixture.ConfirmHandler.Handle(
            new ConfirmPhoneLinkCodeCommand(user.Id, "+79991234567", "123456"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(user.Identities, identity => identity.Type == IdentityType.Phone && identity.Key == "+79991234567");
        Assert.Equal("+7******4567", result.Value.MaskedPhoneNumber);
    }

    [Fact]
    public async Task RequestPhoneLink_WhenPhoneBelongsToAnotherUser_ShouldFail()
    {
        var fixture = CreateFixture();
        var user = User.Create("telegram-user").Value;
        var anotherUser = User.Create("phone-user").Value;
        anotherUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        fixture.Users.Add(user);
        fixture.Users.Add(anotherUser);

        var result = await fixture.RequestHandler.Handle(
            new RequestPhoneLinkCodeCommand(user.Id, "+79991234567"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(fixture.Sender.SentCodes);
    }

    [Fact]
    public async Task RequestPhoneLink_WhenUserAlreadyHasPhone_ShouldFail()
    {
        var fixture = CreateFixture();
        var user = User.Create("phone-user").Value;
        user.AddIdentity(IdentityType.Phone, "+79990000000", "{}");
        fixture.Users.Add(user);

        var result = await fixture.RequestHandler.Handle(
            new RequestPhoneLinkCodeCommand(user.Id, "+79991234567"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(fixture.Sender.SentCodes);
    }

    [Theory]
    [InlineData("79991234567")]
    [InlineData("+7abc9991234567")]
    [InlineData("++79991234567")]
    public async Task RequestPhoneLink_WhenPhoneIsInvalid_ShouldFail(string phoneNumber)
    {
        var fixture = CreateFixture();
        var user = User.Create("telegram-user").Value;
        fixture.Users.Add(user);

        var result = await fixture.RequestHandler.Handle(
            new RequestPhoneLinkCodeCommand(user.Id, phoneNumber),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(fixture.PhoneCodes.Codes);
        Assert.Empty(fixture.Sender.SentCodes);
    }

    [Fact]
    public async Task ConfirmPhoneLink_WhenCodeIsInvalid_ShouldRegisterFailedAttempt()
    {
        var fixture = CreateFixture();
        var user = User.Create("telegram-user").Value;
        fixture.Users.Add(user);
        await fixture.RequestHandler.Handle(
            new RequestPhoneLinkCodeCommand(user.Id, "+79991234567"),
            CancellationToken.None);

        var result = await fixture.ConfirmHandler.Handle(
            new ConfirmPhoneLinkCodeCommand(user.Id, "+79991234567", "000000"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(1, fixture.PhoneCodes.Codes.Single().FailedAttempts);
    }

    [Fact]
    public async Task ConfirmPhoneLink_WhenPhoneBelongsToAnotherUser_ShouldFail()
    {
        var fixture = CreateFixture();
        var targetUser = User.Create("telegram-user").Value;
        targetUser.AddIdentity(IdentityType.Telegram, "278225388", "{}");
        var sourceUser = User.Create("phone-user").Value;
        sourceUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        fixture.Users.Add(targetUser);
        fixture.Users.Add(sourceUser);

        await fixture.RequestHandler.Handle(
            new RequestPhoneLinkCodeCommand(targetUser.Id, "+79991234567"),
            CancellationToken.None);

        var result = await fixture.ConfirmHandler.Handle(
            new ConfirmPhoneLinkCodeCommand(targetUser.Id, "+79991234567", "123456"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Null(sourceUser.DeletedAt);
        Assert.DoesNotContain(targetUser.Identities, identity => identity.Type == IdentityType.Phone && identity.Key == "+79991234567");
    }

    [Fact]
    public async Task ConfirmPhoneChange_WhenCodeIsValid_ShouldReplaceActivePhoneIdentity()
    {
        var fixture = CreateFixture();
        var user = User.Create("phone-user").Value;
        user.AddIdentity(IdentityType.Phone, "+79990000000", "{}");
        user.AddIdentity(IdentityType.Telegram, "278225388", "{}");
        fixture.Users.Add(user);

        await fixture.RequestChangeHandler.Handle(
            new RequestPhoneChangeCodeCommand(user.Id, "+79991234567"),
            CancellationToken.None);

        var result = await fixture.ConfirmChangeHandler.Handle(
            new ConfirmPhoneChangeCodeCommand(user.Id, "+79991234567", "123456"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("+7******4567", result.Value.MaskedPhoneNumber);
        Assert.Contains(user.Identities, identity =>
            identity.Type == IdentityType.Phone
            && identity.Key == "+79990000000"
            && identity.DeletedAt is not null);
        Assert.Contains(user.Identities, identity =>
            identity.Type == IdentityType.Phone
            && identity.Key == "+79991234567"
            && identity.DeletedAt is null);
        Assert.Single(user.Identities.Where(identity =>
            identity.Type == IdentityType.Phone
            && identity.DeletedAt is null));
        Assert.Contains(user.Identities, identity =>
            identity.Type == IdentityType.Telegram
            && identity.Key == "278225388"
            && identity.DeletedAt is null);
    }

    [Fact]
    public async Task RequestPhoneChange_WhenUserHasNoPhone_ShouldFail()
    {
        var fixture = CreateFixture();
        var user = User.Create("telegram-user").Value;
        user.AddIdentity(IdentityType.Telegram, "278225388", "{}");
        fixture.Users.Add(user);

        var result = await fixture.RequestChangeHandler.Handle(
            new RequestPhoneChangeCodeCommand(user.Id, "+79991234567"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(fixture.Sender.SentCodes);
    }

    [Fact]
    public async Task RequestPhoneChange_WhenPhoneBelongsToAnotherUser_ShouldFail()
    {
        var fixture = CreateFixture();
        var user = User.Create("phone-user").Value;
        user.AddIdentity(IdentityType.Phone, "+79990000000", "{}");
        var anotherUser = User.Create("another-phone-user").Value;
        anotherUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        fixture.Users.Add(user);
        fixture.Users.Add(anotherUser);

        var result = await fixture.RequestChangeHandler.Handle(
            new RequestPhoneChangeCodeCommand(user.Id, "+79991234567"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(fixture.Sender.SentCodes);
    }

    [Fact]
    public async Task RequestPhoneChange_WhenNewPhoneIsCurrentPhone_ShouldFail()
    {
        var fixture = CreateFixture();
        var user = User.Create("phone-user").Value;
        user.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        fixture.Users.Add(user);

        var result = await fixture.RequestChangeHandler.Handle(
            new RequestPhoneChangeCodeCommand(user.Id, "+79991234567"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(fixture.Sender.SentCodes);
    }

    private static Fixture CreateFixture()
    {
        var now = new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero);
        var users = new FakeUserRepository();
        var phoneCodes = new FakePhoneAuthCodeRepository();
        var sender = new FakePhoneAuthCodeSender();
        var timeProvider = new FixedTimeProvider(now);
        var phoneAuthCodeService = new PhoneAuthCodeService(
            phoneCodes,
            new FixedPhoneAuthCodeGenerator(),
            sender,
            timeProvider);

        return new Fixture(
            users,
            phoneCodes,
            sender,
            new RequestPhoneLinkCodeHandler(
                users,
                phoneAuthCodeService,
                NullLogger<RequestPhoneLinkCodeHandler>.Instance),
            new ConfirmPhoneLinkCodeHandler(
                users,
                phoneAuthCodeService,
                NullLogger<ConfirmPhoneLinkCodeHandler>.Instance),
            new RequestPhoneChangeCodeHandler(
                users,
                phoneAuthCodeService,
                NullLogger<RequestPhoneChangeCodeHandler>.Instance),
            new ConfirmPhoneChangeCodeHandler(
                users,
                phoneAuthCodeService,
                NullLogger<ConfirmPhoneChangeCodeHandler>.Instance));
    }

    private sealed record Fixture(
        FakeUserRepository Users,
        FakePhoneAuthCodeRepository PhoneCodes,
        FakePhoneAuthCodeSender Sender,
        RequestPhoneLinkCodeHandler RequestHandler,
        ConfirmPhoneLinkCodeHandler ConfirmHandler,
        RequestPhoneChangeCodeHandler RequestChangeHandler,
        ConfirmPhoneChangeCodeHandler ConfirmChangeHandler);

    private sealed class FakePhoneAuthCodeRepository : IPhoneAuthCodeRepository
    {
        public List<PhoneAuthCode> Codes { get; } = [];

        public Task<IReadOnlyCollection<PhoneAuthCode>> GetActiveByPhoneAsync(
            string phoneNumber,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<PhoneAuthCode>>(
                Codes.Where(code => code.PhoneNumber == phoneNumber && code.IsActive(nowUtc)).ToArray());
        }

        public Task<PhoneAuthCode?> GetLatestActiveByPhoneAsync(
            string phoneNumber,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Codes
                .Where(code => code.PhoneNumber == phoneNumber && code.IsActive(nowUtc))
                .OrderByDescending(code => code.CreatedAt)
                .FirstOrDefault());
        }

        public Task<PhoneAuthCode?> GetActiveByIdAsync(
            Guid id,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Codes.FirstOrDefault(code => code.Id == id && code.IsActive(nowUtc)));
        }

        public void Add(PhoneAuthCode code) => Codes.Add(code);

        public Task SaveAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePhoneAuthCodeSender : IPhoneAuthCodeSender
    {
        public List<(string PhoneNumber, string Code)> SentCodes { get; } = [];

        public Task<Result> SendAsync(string phoneNumber, string code, CancellationToken cancellationToken)
        {
            SentCodes.Add((phoneNumber, code));
            return Task.FromResult(Result.Ok());
        }
    }

    private sealed class FixedPhoneAuthCodeGenerator : IPhoneAuthCodeGenerator
    {
        public string Generate() => "123456";
    }
}
