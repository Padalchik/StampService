using FluentResults;
using Microsoft.Extensions.Options;
using StampService.Application.Coins;
using StampService.Application.Metrics;
using StampService.Application.Services;
using StampService.Application.Users;
using StampService.Application.Users.Commands.ConfirmTelegramLinkSession;
using StampService.Application.Users.Commands.RequestTelegramLink;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Coins;
using StampService.Domain.Loyalty;
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
        anotherUser.AddIdentity(IdentityType.Phone, "+79991234568", "{}");
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

    [Fact]
    public async Task ConfirmTelegramLinkSession_WhenSourceHasOnlyTelegramIdentity_ShouldMergeRewardsIntoTargetUser()
    {
        var fixture = CreateFixture();
        var targetUser = User.Create("phone-user").Value;
        targetUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        var sourceUser = User.Create("telegram-user").Value;
        sourceUser.AddIdentity(IdentityType.Telegram, "278225388", "{}");
        fixture.Users.Add(targetUser);
        fixture.Users.Add(sourceUser);

        var brandId = Guid.NewGuid();
        var metricDefinitionId = Guid.NewGuid();
        var sourceWallet = CoinWallet.Create(sourceUser.Id, brandId).Value;
        sourceWallet.Add(15);
        fixture.CoinWallets.Add(sourceWallet);
        var sourceMetricBalance = MetricBalance.Create(sourceUser.Id, brandId, metricDefinitionId).Value;
        sourceMetricBalance.Add(3);
        fixture.MetricBalances.Add(sourceMetricBalance);

        var token = fixture.Protector.Protect(new TelegramLinkSession(
            targetUser.Id,
            new DateTime(2026, 5, 18, 10, 10, 0, DateTimeKind.Utc)));
        var result = await fixture.ConfirmHandler.Handle(
            new ConfirmTelegramLinkSessionCommand(token, 278225388, null, null, "andrey"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(sourceUser.DeletedAt);
        Assert.Contains(targetUser.Identities, identity => identity.Type == IdentityType.Telegram && identity.Key == "278225388");
        Assert.DoesNotContain(sourceUser.Identities, identity => identity.DeletedAt is null);

        var targetWallet = await fixture.CoinWallets.GetByUserAndBrandAsync(targetUser.Id, brandId, CancellationToken.None);
        Assert.NotNull(targetWallet);
        Assert.Equal(15, targetWallet.Value);

        var targetMetricBalance = await fixture.MetricBalances.GetByUserAndMetricAsync(
            targetUser.Id,
            brandId,
            metricDefinitionId,
            CancellationToken.None);
        Assert.NotNull(targetMetricBalance);
        Assert.Equal(3, targetMetricBalance.Value);
    }

    [Fact]
    public async Task ConfirmTelegramLinkSession_WhenSourceHasBrandMembership_ShouldReassignMembershipToTargetUser()
    {
        var fixture = CreateFixture();
        var targetUser = User.Create("phone-user").Value;
        targetUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        var sourceUser = User.Create("telegram-user").Value;
        sourceUser.AddIdentity(IdentityType.Telegram, "278225388", "{}");
        fixture.Users.Add(targetUser);
        fixture.Users.Add(sourceUser);
        fixture.BrandMemberships.SetRole(sourceUser.Id, Guid.NewGuid(), SystemRoles.Staff);

        var token = fixture.Protector.Protect(new TelegramLinkSession(
            targetUser.Id,
            new DateTime(2026, 5, 18, 10, 10, 0, DateTimeKind.Utc)));
        var result = await fixture.ConfirmHandler.Handle(
            new ConfirmTelegramLinkSessionCommand(token, 278225388, null, null, "andrey"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(sourceUser.DeletedAt);
        var targetMemberships = await fixture.BrandMemberships.GetUserMembershipsAsync(
            targetUser.Id,
            CancellationToken.None);
        Assert.Single(targetMemberships);
        var sourceMemberships = await fixture.BrandMemberships.GetUserMembershipsAsync(
            sourceUser.Id,
            CancellationToken.None);
        Assert.Empty(sourceMemberships);
    }

    [Fact]
    public async Task ConfirmTelegramLinkSession_WhenTargetAlreadyHasBrandMembership_ShouldNotMergeAutomatically()
    {
        var fixture = CreateFixture();
        var targetUser = User.Create("phone-user").Value;
        targetUser.AddIdentity(IdentityType.Phone, "+79991234567", "{}");
        var sourceUser = User.Create("telegram-user").Value;
        sourceUser.AddIdentity(IdentityType.Telegram, "278225388", "{}");
        fixture.Users.Add(targetUser);
        fixture.Users.Add(sourceUser);
        var brandId = Guid.NewGuid();
        fixture.BrandMemberships.SetRole(sourceUser.Id, brandId, SystemRoles.Staff);
        fixture.BrandMemberships.SetRole(targetUser.Id, brandId, SystemRoles.Staff);

        var token = fixture.Protector.Protect(new TelegramLinkSession(
            targetUser.Id,
            new DateTime(2026, 5, 18, 10, 10, 0, DateTimeKind.Utc)));
        var result = await fixture.ConfirmHandler.Handle(
            new ConfirmTelegramLinkSessionCommand(token, 278225388, null, null, "andrey"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Null(sourceUser.DeletedAt);
    }

    private static Fixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var protector = new FakeTelegramLinkSessionProtector();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero));
        var telegramOptions = Options.Create(new TelegramOptions { BotUsername = "StampServiceBot" });
        var coinWallets = new FakeCoinWalletRepository();
        var coinTransactions = new FakeCoinTransactionRepository();
        var metricBalances = new FakeMetricBalanceRepository();
        var stampTransactions = new FakeStampTransactionRepository();
        var redemptionCodes = new FakeRedemptionCodeRepository();
        var brandMemberships = new FakeBrandMembershipRepository();
        var autoMergeService = new AutoMergeUserAccountsService(
            brandMemberships,
            coinWallets,
            new CoinLedgerService(coinWallets, coinTransactions),
            metricBalances,
            new MetricLedgerService(metricBalances, stampTransactions),
            redemptionCodes,
            users);

        return new Fixture(
            users,
            protector,
            new RequestTelegramLinkHandler(users, protector, timeProvider, telegramOptions),
            new ConfirmTelegramLinkSessionHandler(
                users,
                protector,
                timeProvider,
                autoMergeService),
            coinWallets,
            metricBalances,
            brandMemberships);
    }

    private sealed record Fixture(
        FakeUserRepository Users,
        FakeTelegramLinkSessionProtector Protector,
        RequestTelegramLinkHandler RequestHandler,
        ConfirmTelegramLinkSessionHandler ConfirmHandler,
        FakeCoinWalletRepository CoinWallets,
        FakeMetricBalanceRepository MetricBalances,
        FakeBrandMembershipRepository BrandMemberships);

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
