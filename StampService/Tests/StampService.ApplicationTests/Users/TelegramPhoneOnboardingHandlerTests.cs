using FluentResults;
using StampService.Application.Auth;
using StampService.Application.Users;
using StampService.Application.Users.Commands.ConfirmTelegramPhoneCode;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Users;

public class TelegramPhoneOnboardingHandlerTests
{
    [Fact]
    public async Task ConfirmTelegramPhoneCode_WhenPhoneIsNew_ShouldCreatePhoneUserAndLinkTelegram()
    {
        var fixture = CreateFixture();
        await fixture.PhoneAuthCodeService.RequestCodeAsync(
            "+79991234567",
            invalidField: null,
            cancellationToken: CancellationToken.None);

        var result = await fixture.Handler.Handle(
            new ConfirmTelegramPhoneCodeCommand(
                278225388,
                "Andrey",
                null,
                "andrey",
                "+79991234567",
                "123456"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = Assert.Single(fixture.Users.Users);
        Assert.Contains(user.Identities, identity => identity.Type == IdentityType.Phone && identity.Key == "+79991234567");
        Assert.Contains(user.Identities, identity => identity.Type == IdentityType.Telegram && identity.Key == "278225388");
    }

    [Fact]
    public async Task ConfirmTelegramPhoneCode_WhenPhoneUserAlreadyExists_ShouldLinkTelegramToPhoneUser()
    {
        var fixture = CreateFixture();
        var phoneUser = User.Create("phone-user").Value;
        phoneUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        fixture.Users.Add(phoneUser);

        await fixture.PhoneAuthCodeService.RequestCodeAsync(
            "+79991234567",
            invalidField: null,
            cancellationToken: CancellationToken.None);

        var result = await fixture.Handler.Handle(
            new ConfirmTelegramPhoneCodeCommand(
                278225388,
                "Andrey",
                null,
                "andrey",
                "+79991234567",
                "123456"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(fixture.Users.Users);
        Assert.Contains(phoneUser.Identities, identity => identity.Type == IdentityType.Telegram && identity.Key == "278225388");
    }

    [Fact]
    public async Task ConfirmTelegramPhoneCode_WhenTelegramBelongsToLegacyTelegramOnlyUser_ShouldFail()
    {
        var fixture = CreateFixture();
        var phoneUser = User.Create("phone-user").Value;
        phoneUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        var legacyTelegramUser = User.Create("legacy-telegram-user").Value;
        legacyTelegramUser.AddIdentity(IdentityType.Telegram, "278225388", "{}");
        fixture.Users.Add(phoneUser);
        fixture.Users.Add(legacyTelegramUser);

        await fixture.PhoneAuthCodeService.RequestCodeAsync(
            "+79991234567",
            invalidField: null,
            cancellationToken: CancellationToken.None);

        var result = await fixture.Handler.Handle(
            new ConfirmTelegramPhoneCodeCommand(
                278225388,
                "Andrey",
                null,
                "andrey",
                "+79991234567",
                "123456"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.DoesNotContain(phoneUser.Identities, identity => identity.Type == IdentityType.Telegram && identity.Key == "278225388");
        Assert.Contains(legacyTelegramUser.Identities, identity =>
            identity.DeletedAt is null
            && identity.Type == IdentityType.Telegram
            && identity.Key == "278225388");
        Assert.Null(legacyTelegramUser.DeletedAt);
    }

    [Fact]
    public async Task ConfirmTelegramPhoneCode_WhenTelegramBelongsToAnotherPhoneUser_ShouldFail()
    {
        var fixture = CreateFixture();
        var existingUser = User.Create("existing").Value;
        existingUser.AddIdentity(IdentityType.Phone, "+79990000000", "{}");
        existingUser.AddIdentity(IdentityType.Telegram, "278225388", "{}");
        fixture.Users.Add(existingUser);

        await fixture.PhoneAuthCodeService.RequestCodeAsync(
            "+79991234567",
            invalidField: null,
            cancellationToken: CancellationToken.None);

        var result = await fixture.Handler.Handle(
            new ConfirmTelegramPhoneCodeCommand(
                278225388,
                "Andrey",
                null,
                "andrey",
                "+79991234567",
                "123456"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Single(fixture.Users.Users);
        Assert.True(fixture.PhoneAuthCodeServiceCodes.Single().IsActive(fixture.Now.UtcDateTime));
        Assert.DoesNotContain(
            fixture.Users.Users,
            user => user.Identities.Any(identity => identity.Type == IdentityType.Phone && identity.Key == "+79991234567"));
    }

    [Fact]
    public async Task ConfirmTelegramPhoneCode_WhenPhoneUserAlreadyHasTelegram_ShouldFail()
    {
        var fixture = CreateFixture();
        var phoneUser = User.Create("phone-user").Value;
        phoneUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        phoneUser.AddIdentity(IdentityType.Telegram, "278225389", "{}");
        fixture.Users.Add(phoneUser);

        await fixture.PhoneAuthCodeService.RequestCodeAsync(
            "+79991234567",
            invalidField: null,
            cancellationToken: CancellationToken.None);

        var result = await fixture.Handler.Handle(
            new ConfirmTelegramPhoneCodeCommand(
                278225388,
                "Andrey",
                null,
                "andrey",
                "+79991234567",
                "123456"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Contains(phoneUser.Identities, identity =>
            identity.Type == IdentityType.Telegram
            && identity.Key == "278225389"
            && identity.DeletedAt is null);
        Assert.DoesNotContain(phoneUser.Identities, identity => identity.Type == IdentityType.Telegram && identity.Key == "278225388");
        Assert.True(fixture.PhoneAuthCodeServiceCodes.Single().IsActive(fixture.Now.UtcDateTime));
    }

    private static Fixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var phoneCodes = new FakePhoneAuthCodeRepository();
        var sender = new FakePhoneAuthCodeSender();
        var phoneAuthCodeService = new PhoneAuthCodeService(
            phoneCodes,
            new FixedPhoneAuthCodeGenerator(),
            sender,
            new FixedTimeProvider(new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero)));
        var phoneAccountService = new PhoneAccountService(users, new CustomerCodeGenerator(users));

        return new Fixture(
            users,
            phoneAuthCodeService,
            phoneCodes.Codes,
            new DateTimeOffset(2026, 5, 17, 10, 0, 0, TimeSpan.Zero),
            new ConfirmTelegramPhoneCodeHandler(
                users,
                phoneAuthCodeService,
                phoneAccountService));
    }

    private sealed record Fixture(
        FakeUserRepository Users,
        PhoneAuthCodeService PhoneAuthCodeService,
        List<PhoneAuthCode> PhoneAuthCodeServiceCodes,
        DateTimeOffset Now,
        ConfirmTelegramPhoneCodeHandler Handler);

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
        public Task<Result> SendAsync(string phoneNumber, string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Ok());
        }
    }

    private sealed class FixedPhoneAuthCodeGenerator : IPhoneAuthCodeGenerator
    {
        public string Generate() => "123456";
    }
}
