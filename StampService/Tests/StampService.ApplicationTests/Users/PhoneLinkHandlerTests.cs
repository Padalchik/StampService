using FluentResults;
using Microsoft.Extensions.Logging.Abstractions;
using StampService.Application.Auth;
using StampService.Application.Users.Commands.ConfirmPhoneLinkCode;
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

    private static Fixture CreateFixture()
    {
        var now = new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero);
        var users = new FakeUserRepository();
        var phoneCodes = new FakePhoneAuthCodeRepository();
        var sender = new FakePhoneAuthCodeSender();
        var timeProvider = new FixedTimeProvider(now);

        return new Fixture(
            users,
            phoneCodes,
            sender,
            new RequestPhoneLinkCodeHandler(
                users,
                phoneCodes,
                new FixedPhoneAuthCodeGenerator(),
                sender,
                timeProvider,
                NullLogger<RequestPhoneLinkCodeHandler>.Instance),
            new ConfirmPhoneLinkCodeHandler(
                users,
                phoneCodes,
                timeProvider,
                NullLogger<ConfirmPhoneLinkCodeHandler>.Instance));
    }

    private sealed record Fixture(
        FakeUserRepository Users,
        FakePhoneAuthCodeRepository PhoneCodes,
        FakePhoneAuthCodeSender Sender,
        RequestPhoneLinkCodeHandler RequestHandler,
        ConfirmPhoneLinkCodeHandler ConfirmHandler);

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
