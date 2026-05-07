using StampService.Application.Users.Commands.CreateRedemptionCode;
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
            new FixedRedemptionCodeGenerator("123456"),
            codeRepository,
            new FixedTimeProvider(now),
            userRepository);

        var result = await handler.Handle(
            new CreateRedemptionCodeCommand(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("123456", result.Value.Code);
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
            "111111",
            now.UtcDateTime.AddMinutes(2),
            now.UtcDateTime).Value);

        var handler = new CreateRedemptionCodeHandler(
            new FixedRedemptionCodeGenerator("222222"),
            codeRepository,
            new FixedTimeProvider(now),
            userRepository);

        var result = await handler.Handle(
            new CreateRedemptionCodeCommand(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("111111", result.Value.Code);
        Assert.Single(codeRepository.Codes);
        Assert.Equal(0, codeRepository.SaveCount);
    }
}
