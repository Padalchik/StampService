using StampService.Application.Users.Commands.CreateRedemptionCode;
using StampService.Application.Users;
using StampService.Application.Errors;
using StampService.ApplicationTests.Fakes;
using DomainUser = StampService.Domain.User.User;

namespace StampService.ApplicationTests.Users;

public class CreateRedemptionCodeHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserExists_ShouldCreateRedemptionCode()
    {
        var now = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
        var user = DomainUser.Create("Ivan", "1234").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var codeRepository = new FakeRedemptionCodeRepository();
        var handler = new CreateRedemptionCodeHandler(
            new FixedRedemptionCodeGenerator("1234"),
            codeRepository,
            new FixedTimeProvider(now),
            userRepository);

        var result = await handler.Handle(
            new CreateRedemptionCodeCommand(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("1234", result.Value.Code);
        Assert.Equal(now.UtcDateTime.AddMinutes(3), result.Value.ExpiresAtUtc);
        Assert.Single(codeRepository.Codes);
        Assert.Equal(1, codeRepository.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenActiveCodeAlreadyExists_ShouldReturnExistingCode()
    {
        var now = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
        var user = DomainUser.Create("Ivan", "1234").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var codeRepository = new FakeRedemptionCodeRepository();
        codeRepository.Add(StampService.Domain.User.RedemptionCode.Create(
            user.Id,
            "1111",
            now.UtcDateTime.AddMinutes(2),
            now.UtcDateTime).Value);

        var handler = new CreateRedemptionCodeHandler(
            new FixedRedemptionCodeGenerator("2222"),
            codeRepository,
            new FixedTimeProvider(now),
            userRepository);

        var result = await handler.Handle(
            new CreateRedemptionCodeCommand(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("1111", result.Value.Code);
        Assert.Single(codeRepository.Codes);
        Assert.Equal(0, codeRepository.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenForceRefreshAndActiveCodeExists_ShouldExpireExistingCodeAndCreateNewCode()
    {
        var now = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
        var user = DomainUser.Create("Ivan", "1234").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var codeRepository = new FakeRedemptionCodeRepository();
        var existingCode = StampService.Domain.User.RedemptionCode.Create(
            user.Id,
            "1111",
            now.UtcDateTime.AddMinutes(2),
            now.UtcDateTime).Value;
        codeRepository.Add(existingCode);

        var handler = new CreateRedemptionCodeHandler(
            new FixedRedemptionCodeGenerator("2222"),
            codeRepository,
            new FixedTimeProvider(now),
            userRepository);

        var result = await handler.Handle(
            new CreateRedemptionCodeCommand(user.Id, ForceRefresh: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("2222", result.Value.Code);
        Assert.Equal(now.UtcDateTime, existingCode.ExpiresAtUtc);
        Assert.False(existingCode.IsActive(now.UtcDateTime));
        Assert.False(await codeRepository.ActiveCodeExistsAsync("1111", now.UtcDateTime, CancellationToken.None));
        Assert.Equal(2, codeRepository.Codes.Count);
        Assert.Equal(2, codeRepository.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenCodePoolIsExhausted_ShouldFail()
    {
        var now = new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);
        var user = DomainUser.Create("Ivan", "1234").Value;
        var userRepository = new FakeUserRepository();
        userRepository.Add(user);
        var codeRepository = new FakeRedemptionCodeRepository();
        var handler = new CreateRedemptionCodeHandler(
            new ExhaustedRedemptionCodeGenerator(),
            codeRepository,
            new FixedTimeProvider(now),
            userRepository);

        var result = await handler.Handle(
            new CreateRedemptionCodeCommand(user.Id),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        var error = Assert.IsType<AppError>(result.Errors.Single());
        Assert.Equal(AppErrorCodes.RedemptionCode.PoolExhausted, error.Code);
        Assert.Empty(codeRepository.Codes);
        Assert.Equal(0, codeRepository.SaveCount);
    }

    private sealed class ExhaustedRedemptionCodeGenerator : IRedemptionCodeGenerator
    {
        public Task<string> GenerateAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("No codes");
        }
    }
}
