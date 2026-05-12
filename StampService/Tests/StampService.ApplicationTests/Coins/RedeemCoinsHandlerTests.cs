using StampService.Application.Access;
using StampService.Application.Coins;
using StampService.Application.Coins.Commands.RedeemCoins;
using StampService.Application.Users.Commands.UseRedemptionCode;
using StampService.ApplicationTests.Fakes;
using StampService.Domain.Access;
using StampService.Domain.Coins;
using StampService.Domain.User;

namespace StampService.ApplicationTests.Coins;

public class RedeemCoinsHandlerTests
{
    [Fact]
    public async Task Handle_WhenRedemptionCodeIsActiveAndBalanceIsEnough_ShouldRedeemCoinsAndConsumeCode()
    {
        var now = new DateTimeOffset(2026, 5, 9, 10, 0, 0, TimeSpan.Zero);
        var brandId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var customer = User.Create("Customer", "1234").Value;
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var userRepository = new FakeUserRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var codeRepository = new FakeRedemptionCodeRepository();
        brandRepository.AddExisting(brandId);
        membershipRepository.SetRole(actorUserId, brandId, SystemRoles.Staff);
        userRepository.Add(customer);
        var wallet = CoinWallet.Create(customer.Id, brandId).Value;
        wallet.Add(10);
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, 10, "Existing issue").Value);
        var redemptionCode = RedemptionCode.Create(
            customer.Id,
            "1234",
            now.UtcDateTime.AddMinutes(3),
            now.UtcDateTime).Value;
        codeRepository.Add(redemptionCode);

        var handler = new RedeemCoinsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            new CoinLedgerService(walletRepository, transactionRepository),
            transactionRepository,
            walletRepository,
            codeRepository,
            userRepository,
            new UseRedemptionCodeHandler(codeRepository, new FixedTimeProvider(now)),
            new FixedTimeProvider(now));

        var result = await handler.Handle(
            new RedeemCoinsCommand(brandId, actorUserId, "1234", 4, "Free product"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.UserId);
        Assert.Equal(4, result.Value.Amount);
        Assert.Equal(6, result.Value.BalanceValue);
        Assert.NotNull(redemptionCode.UsedAtUtc);
        Assert.Equal(2, transactionRepository.Transactions.Count);
    }

    [Fact]
    public async Task Handle_WhenBalanceIsInsufficient_ShouldFailAndKeepCodeActive()
    {
        var now = new DateTimeOffset(2026, 5, 9, 10, 0, 0, TimeSpan.Zero);
        var brandId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var customer = User.Create("Customer", "1234").Value;
        var brandRepository = new FakeBrandRepository();
        var membershipRepository = new FakeBrandMembershipRepository();
        var userRepository = new FakeUserRepository();
        var walletRepository = new FakeCoinWalletRepository();
        var transactionRepository = new FakeCoinTransactionRepository();
        var codeRepository = new FakeRedemptionCodeRepository();
        brandRepository.AddExisting(brandId);
        membershipRepository.SetRole(actorUserId, brandId, SystemRoles.Staff);
        userRepository.Add(customer);
        var wallet = CoinWallet.Create(customer.Id, brandId).Value;
        wallet.Add(2);
        walletRepository.Add(wallet);
        transactionRepository.Add(CoinTransaction.CreateIssue(wallet.Id, 2, "Existing issue").Value);
        var redemptionCode = RedemptionCode.Create(
            customer.Id,
            "1234",
            now.UtcDateTime.AddMinutes(3),
            now.UtcDateTime).Value;
        codeRepository.Add(redemptionCode);

        var handler = new RedeemCoinsHandler(
            new BrandAccessService(membershipRepository),
            brandRepository,
            new CoinLedgerService(walletRepository, transactionRepository),
            transactionRepository,
            walletRepository,
            codeRepository,
            userRepository,
            new UseRedemptionCodeHandler(codeRepository, new FixedTimeProvider(now)),
            new FixedTimeProvider(now));

        var result = await handler.Handle(
            new RedeemCoinsCommand(brandId, actorUserId, "1234", 4, "Free product"),
            CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Null(redemptionCode.UsedAtUtc);
        Assert.Single(transactionRepository.Transactions);
        Assert.Equal(2, wallet.Value);
    }
}
