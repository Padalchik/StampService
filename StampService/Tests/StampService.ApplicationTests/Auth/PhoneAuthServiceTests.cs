using FluentResults;
using StampService.Application.Auth;
using StampService.Application.Services;
using StampService.Application.Users;
using StampService.ApplicationTests.Fakes;
using StampService.Contracts.DTOs.Auth;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Auth;

public class PhoneAuthServiceTests
{
    [Fact]
    public async Task RequestPhoneCode_WhenPhoneIsValid_ShouldPersistAndSendCode()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.RequestPhoneCodeAsync(
            new RequestPhoneAuthCodeRequest("+7 (999) 123-45-67"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.Now.AddMinutes(10).UtcDateTime, result.Value.ExpiresAt);
        var code = Assert.Single(fixture.PhoneCodes.Codes);
        Assert.Equal("+79991234567", code.PhoneNumber);
        Assert.Equal("123456", code.Code);
        Assert.Equal(1, fixture.PhoneCodes.SaveCount);
        Assert.Equal(("+79991234567", "123456", false), Assert.Single(fixture.Sender.SentCodes));
    }

    [Fact]
    public async Task RequestPhoneCode_WhenSmsRequestedAndDisabled_ShouldFailWithoutPersistingCode()
    {
        var fixture = CreateFixture(smsEnabled: false);

        var result = await fixture.Service.RequestPhoneCodeAsync(
            new RequestPhoneAuthCodeRequest("+7 (999) 123-45-67", SendSms: true),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, error => error.Metadata["error_code"].Equals("auth.phone_sms_disabled"));
        Assert.Empty(fixture.PhoneCodes.Codes);
        Assert.Empty(fixture.Sender.SentCodes);
    }

    [Fact]
    public async Task RequestPhoneCode_WhenSmsRequestedAndEnabled_ShouldPassSmsFlagToSender()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.RequestPhoneCodeAsync(
            new RequestPhoneAuthCodeRequest("+7 (999) 123-45-67", SendSms: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(("+79991234567", "123456", true), Assert.Single(fixture.Sender.SentCodes));
    }

    [Theory]
    [InlineData("79991234567")]
    [InlineData("+7abc9991234567")]
    [InlineData("++79991234567")]
    public async Task RequestPhoneCode_WhenPhoneIsInvalid_ShouldFail(string phoneNumber)
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.RequestPhoneCodeAsync(
            new RequestPhoneAuthCodeRequest(phoneNumber),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(fixture.PhoneCodes.Codes);
        Assert.Empty(fixture.Sender.SentCodes);
    }

    [Fact]
    public async Task VerifyPhoneCode_WhenCodeIsValidAndUserDoesNotExist_ShouldCreatePhoneUser()
    {
        var fixture = CreateFixture();
        await fixture.Service.RequestPhoneCodeAsync(
            new RequestPhoneAuthCodeRequest("+79991234567"),
            CancellationToken.None);

        var result = await fixture.Service.VerifyPhoneCodeAsync(
            new VerifyPhoneAuthCodeRequest("+79991234567", "123456"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("token", result.Value.Token);
        var user = Assert.Single(fixture.Users.Users);
        Assert.StartsWith("Неопознанн", user.Name, StringComparison.Ordinal);
        Assert.Contains(
            user.Identities,
            identity => identity.Type == IdentityType.Phone && identity.Key == "+79991234567");
        Assert.True(fixture.PhoneCodes.Codes.Single().UsedAtUtc.HasValue);
    }

    [Fact]
    public async Task VerifyPhoneCode_WhenCodeIsInvalid_ShouldRegisterFailedAttempt()
    {
        var fixture = CreateFixture();
        await fixture.Service.RequestPhoneCodeAsync(
            new RequestPhoneAuthCodeRequest("+79991234567"),
            CancellationToken.None);

        var result = await fixture.Service.VerifyPhoneCodeAsync(
            new VerifyPhoneAuthCodeRequest("+79991234567", "000000"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Equal(1, fixture.PhoneCodes.Codes.Single().FailedAttempts);
        Assert.Equal(2, fixture.PhoneCodes.SaveCount);
        Assert.Empty(fixture.Users.Users);
    }

    private static Fixture CreateFixture(bool smsEnabled = true)
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
        var phoneAccountService = new PhoneAccountService(
            users,
            new CuteUserDisplayNameGenerator());
        var smsSettings = new FakePhoneAuthSmsSettingsRepository(smsEnabled);
        var service = new AuthService(
            users,
            new FakeJwtTokenService(),
            new AlwaysValidTelegramValidationService(),
            phoneAuthCodeService,
            phoneAccountService,
            smsSettings);

        return new Fixture(service, users, phoneCodes, sender, now);
    }

    private sealed record Fixture(
        AuthService Service,
        FakeUserRepository Users,
        FakePhoneAuthCodeRepository PhoneCodes,
        FakePhoneAuthCodeSender Sender,
        DateTimeOffset Now);

    private sealed class FakePhoneAuthCodeRepository : IPhoneAuthCodeRepository
    {
        public List<PhoneAuthCode> Codes { get; } = [];
        public int SaveCount { get; private set; }

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
            var code = Codes
                .Where(item => item.PhoneNumber == phoneNumber && item.IsActive(nowUtc))
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefault();

            return Task.FromResult(code);
        }

        public Task<PhoneAuthCode?> GetActiveByIdAsync(
            Guid id,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Codes.FirstOrDefault(code => code.Id == id && code.IsActive(nowUtc)));
        }

        public void Add(PhoneAuthCode code)
        {
            Codes.Add(code);
        }

        public Task SaveAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePhoneAuthCodeSender : IPhoneAuthCodeSender
    {
        public List<(string PhoneNumber, string Code, bool SendSms)> SentCodes { get; } = [];

        public Task<Result> SendAsync(
            string phoneNumber,
            string code,
            bool sendSms,
            CancellationToken cancellationToken)
        {
            SentCodes.Add((phoneNumber, code, sendSms));
            return Task.FromResult(Result.Ok());
        }
    }

    private sealed class FakePhoneAuthSmsSettingsRepository : IPhoneAuthSmsSettingsRepository
    {
        private readonly PhoneAuthSmsSettings _settings;

        public FakePhoneAuthSmsSettingsRepository(bool isEnabled)
        {
            _settings = PhoneAuthSmsSettings.Create(isEnabled);
        }

        public Task<PhoneAuthSmsSettings> GetOrCreateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_settings);
        }

        public Task SaveAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FixedPhoneAuthCodeGenerator : IPhoneAuthCodeGenerator
    {
        public string Generate() => "123456";
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public JwtToken CreateToken(User user) => new("token", new DateTime(2026, 5, 18, 0, 0, 0, DateTimeKind.Utc));
    }

    private sealed class AlwaysValidTelegramValidationService : ITelegramValidationService
    {
        public bool Validate(TelegramLoginRequest request) => true;
    }
}
