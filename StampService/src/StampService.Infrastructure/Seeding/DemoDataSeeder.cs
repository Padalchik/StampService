using FluentResults;
using Microsoft.EntityFrameworkCore;
using StampService.Domain.Access;
using StampService.Domain.Brand;
using StampService.Domain.Coins;
using StampService.Domain.CustomerNotifications;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.Infrastructure.Seeding;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        long adminTelegramUserId,
        CancellationToken cancellationToken = default)
    {
        if (adminTelegramUserId <= 0)
            throw new InvalidOperationException("Telegram id администратора должен быть положительным числом.");

        var hasBusinessData = await dbContext.Users.AnyAsync(cancellationToken)
            || await dbContext.Brands.AnyAsync(cancellationToken);
        if (hasBusinessData)
        {
            throw new InvalidOperationException(
                "Заполнение начальными данными ожидает пустую базу. Пользователи или бренды уже существуют. Данные не изменены.");
        }

        await RoleSeeder.SeedSystemRolesAsync(dbContext, cancellationToken);

        var ownerRoleId = await GetRoleIdAsync(dbContext, SystemRoles.Owner, cancellationToken);
        var owner = CreateUser("Владелец брендов", "+70000001001", adminTelegramUserId);

        var allRewardsBrand = CreateBrand(
            "Кофейная лаборатория",
            isMetricsEnabled: true,
            isCoinsEnabled: true,
            isCoinProductRedemptionEnabled: true,
            isManualCoinRedemptionEnabled: true);
        var coinsOnlyBrand = CreateBrand(
            "Монетный уголок",
            isMetricsEnabled: false,
            isCoinsEnabled: true,
            isCoinProductRedemptionEnabled: true,
            isManualCoinRedemptionEnabled: false);
        var metricsOnlyBrand = CreateBrand(
            "Пекарня штампов",
            isMetricsEnabled: true,
            isCoinsEnabled: false,
            isCoinProductRedemptionEnabled: false,
            isManualCoinRedemptionEnabled: false);

        dbContext.Users.Add(owner);
        dbContext.Brands.AddRange(allRewardsBrand, coinsOnlyBrand, metricsOnlyBrand);

        dbContext.BrandMemberships.AddRange(
            CreateMembership(owner.Id, allRewardsBrand.Id, ownerRoleId),
            CreateMembership(owner.Id, coinsOnlyBrand.Id, ownerRoleId),
            CreateMembership(owner.Id, metricsOnlyBrand.Id, ownerRoleId));

        dbContext.BrandCustomers.AddRange(
            CreateBrandCustomer(allRewardsBrand.Id, owner.Id, owner.Id),
            CreateBrandCustomer(coinsOnlyBrand.Id, owner.Id, owner.Id),
            CreateBrandCustomer(metricsOnlyBrand.Id, owner.Id, owner.Id));

        var coffeeMetric = CreateMetric(allRewardsBrand.Id, "Штамп за кофе", redemptionAmount: 6);
        var dessertMetric = CreateMetric(allRewardsBrand.Id, "Штамп за десерт", redemptionAmount: 4);
        var bakeryMetric = CreateMetric(metricsOnlyBrand.Id, "Штамп за хлеб", redemptionAmount: 5);

        dbContext.LoyaltyMetricDefinitions.AddRange(coffeeMetric, dessertMetric, bakeryMetric);

        dbContext.CoinProducts.AddRange(
            CreateCoinProduct(allRewardsBrand.Id, "Американо", price: 8),
            CreateCoinProduct(allRewardsBrand.Id, "Капучино", price: 12),
            CreateCoinProduct(coinsOnlyBrand.Id, "Подарочный напиток", price: 10),
            CreateCoinProduct(coinsOnlyBrand.Id, "Фирменная кружка", price: 25));

        AddCoinScenario(dbContext, owner.Id, allRewardsBrand.Id, owner.Id, 18, "Приветственный бонус");
        AddCoinScenario(dbContext, owner.Id, coinsOnlyBrand.Id, owner.Id, 12, "Начисление за покупку");

        AddMetricScenario(dbContext, owner.Id, allRewardsBrand.Id, coffeeMetric.Id, owner.Id, 7, "Визиты за кофе");
        AddMetricScenario(dbContext, owner.Id, allRewardsBrand.Id, dessertMetric.Id, owner.Id, 3, "Заказы десертов");
        AddMetricScenario(dbContext, owner.Id, metricsOnlyBrand.Id, bakeryMetric.Id, owner.Id, 6, "Покупки хлеба");

        if (!await dbContext.RewardDigestSettings.AnyAsync(cancellationToken))
            dbContext.RewardDigestSettings.Add(CreateRewardDigestSettings());

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Guid> GetRoleIdAsync(
        AppDbContext dbContext,
        string systemName,
        CancellationToken cancellationToken)
    {
        return await dbContext.Roles
            .Where(role => role.SystemName == systemName)
            .Select(role => role.Id)
            .SingleAsync(cancellationToken);
    }

    private static User CreateUser(string name, string phoneNumber, long telegramUserId)
    {
        var user = Require(User.Create(name), $"Не удалось создать пользователя '{name}'.");
        Require(
            user.AddIdentity(IdentityType.Phone, phoneNumber, $"{{\"phoneNumber\":\"{phoneNumber}\",\"seeded\":true}}"),
            $"Не удалось добавить телефон пользователю '{name}'.");

        var telegramId = telegramUserId.ToString();
        Require(
            user.AddIdentity(IdentityType.Telegram, telegramId, $"{{\"telegramId\":\"{telegramId}\"}}"),
            $"Не удалось добавить Telegram-идентификатор пользователю '{name}'.");

        return user;
    }

    private static Brand CreateBrand(
        string name,
        bool isMetricsEnabled,
        bool isCoinsEnabled,
        bool isCoinProductRedemptionEnabled,
        bool isManualCoinRedemptionEnabled)
    {
        var brand = Require(Brand.Create(name), $"Не удалось создать бренд '{name}'.");
        Require(
            brand.UpdateDetails(
                name,
                isMetricsEnabled,
                isCoinsEnabled,
                isCoinProductRedemptionEnabled,
                isManualCoinRedemptionEnabled),
            $"Не удалось обновить бренд '{name}'.");

        return brand;
    }

    private static BrandMembership CreateMembership(Guid userId, Guid brandId, Guid roleId)
    {
        return Require(
            BrandMembership.Create(userId, brandId, roleId),
            "Не удалось создать связь пользователя с брендом.");
    }

    private static BrandCustomer CreateBrandCustomer(Guid brandId, Guid userId, Guid createdByUserId)
    {
        return Require(
            BrandCustomer.Create(brandId, userId, createdByUserId),
            "Could not create brand customer link.");
    }

    private static LoyaltyMetricDefinition CreateMetric(Guid brandId, string name, int redemptionAmount)
    {
        return Require(
            LoyaltyMetricDefinition.Create(brandId, name, redemptionAmount),
            $"Не удалось создать метрику '{name}'.");
    }

    private static CoinProduct CreateCoinProduct(Guid brandId, string name, int price)
    {
        return Require(
            CoinProduct.Create(brandId, name, price),
            $"Не удалось создать товар '{name}'.");
    }

    private static void AddCoinScenario(
        AppDbContext dbContext,
        Guid userId,
        Guid brandId,
        Guid actorUserId,
        int amount,
        string comment)
    {
        var wallet = Require(CoinWallet.Create(userId, brandId), "Не удалось создать кошелёк монеток.");
        Require(wallet.Add(amount), "Не удалось начислить бонусы в кошелёк.");

        var transaction = Require(
            CoinTransaction.CreateIssue(wallet.Id, amount, comment, actorUserId),
            "Не удалось создать транзакцию монеток.");

        dbContext.CoinWallets.Add(wallet);
        dbContext.CoinTransactions.Add(transaction);
    }

    private static void AddMetricScenario(
        AppDbContext dbContext,
        Guid userId,
        Guid brandId,
        Guid metricDefinitionId,
        Guid actorUserId,
        int amount,
        string comment)
    {
        var balance = Require(
            MetricBalance.Create(userId, brandId, metricDefinitionId),
            "Не удалось создать баланс метрики.");
        Require(balance.Add(amount), "Не удалось начислить значение метрики.");

        var transaction = Require(
            StampTransaction.CreateIssue(balance.Id, amount, comment, actorUserId),
            "Не удалось создать транзакцию метрики.");

        dbContext.MetricBalances.Add(balance);
        dbContext.StampTransactions.Add(transaction);
    }

    private static RewardDigestSettings CreateRewardDigestSettings()
    {
        return Require(
            RewardDigestSettings.Create(
                enabled: true,
                messageToUserIntervalMinutes: 60,
                scanIntervalMinutes: 15,
                batchSize: 50,
                maxBrandsPerMessage: 3,
                maxRewardsPerBrand: 3),
            "Не удалось создать настройки дайджеста наград.");
    }

    private static T Require<T>(Result<T> result, string message)
    {
        if (result.IsSuccess)
            return result.Value;

        throw new InvalidOperationException($"{message} {FormatErrors(result.Errors)}");
    }

    private static void Require(Result result, string message)
    {
        if (result.IsSuccess)
            return;

        throw new InvalidOperationException($"{message} {FormatErrors(result.Errors)}");
    }

    private static string FormatErrors(IReadOnlyCollection<IError> errors)
    {
        return string.Join("; ", errors.Select(error => error.Message));
    }
}
