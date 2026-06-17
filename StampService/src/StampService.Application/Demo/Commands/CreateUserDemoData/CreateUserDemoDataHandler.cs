using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Administration;
using StampService.Application.Auth;
using StampService.Application.Brands;
using StampService.Application.CoinProducts;
using StampService.Application.Coins;
using StampService.Application.Demo;
using StampService.Application.Errors;
using StampService.Application.Metrics;
using StampService.Application.Users;
using StampService.Domain.Access;
using StampService.Domain.Coins;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.Application.Demo.Commands.CreateUserDemoData;

public class CreateUserDemoDataHandler : ICommandHandler<bool, CreateUserDemoDataCommand>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IBrandCustomerService _brandCustomerService;
    private readonly IBrandMembershipRepository _brandMembershipRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly ICoinLedgerService _coinLedgerService;
    private readonly ICoinProductRepository _coinProductRepository;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IMetricLedgerService _metricLedgerService;
    private readonly IUserRepository _userRepository;

    public CreateUserDemoDataHandler(
        IAdminAccessService adminAccessService,
        IBrandCustomerService brandCustomerService,
        IBrandMembershipRepository brandMembershipRepository,
        IBrandRepository brandRepository,
        ICoinLedgerService coinLedgerService,
        ICoinProductRepository coinProductRepository,
        ILoyaltyMetricRepository metricRepository,
        IMetricLedgerService metricLedgerService,
        IUserRepository userRepository)
    {
        _adminAccessService = adminAccessService;
        _brandCustomerService = brandCustomerService;
        _brandMembershipRepository = brandMembershipRepository;
        _brandRepository = brandRepository;
        _coinLedgerService = coinLedgerService;
        _coinProductRepository = coinProductRepository;
        _metricRepository = metricRepository;
        _metricLedgerService = metricLedgerService;
        _userRepository = userRepository;
    }

    public async Task<Result<bool>> Handle(
        CreateUserDemoDataCommand command,
        CancellationToken cancellationToken)
    {
        if (!await _adminAccessService.IsAdminAsync(command.Admin, cancellationToken))
            return Result.Fail(AccessErrors.AdminRequired());

        var admin = await GetAdminUserAsync(command.Admin, cancellationToken);
        if (admin is null)
            return Result.Fail(UserErrors.NotFound());

        var role = await _brandMembershipRepository.GetRoleSystemNameAsync(
            admin.Id,
            command.BrandId,
            cancellationToken);
        if (role != SystemRoles.Owner)
            return Result.Fail(AccessErrors.Denied());

        var brand = await _brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
            return Result.Fail(BrandErrors.NotFound());

        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            command.PhoneNumber,
            nameof(command.PhoneNumber));
        if (phoneNumberResult.IsFailed)
            return Result.Fail(phoneNumberResult.Errors);

        var customer = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            phoneNumberResult.Value,
            cancellationToken);
        if (customer is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        var customerLinkResult = await _brandCustomerService.EnsureAsync(
            command.BrandId,
            customer.Id,
            admin.Id,
            cancellationToken);
        if (customerLinkResult.IsFailed)
            return Result.Fail(customerLinkResult.Errors);

        var metricsResult = await EnsureMetricsAsync(command.BrandId, cancellationToken);
        if (metricsResult.IsFailed)
            return Result.Fail(metricsResult.Errors);

        var productsResult = await EnsureProductsAsync(command.BrandId, cancellationToken);
        if (productsResult.IsFailed)
            return Result.Fail(productsResult.Errors);

        var ledgerResult = await CreateLedgerAsync(
            customer.Id,
            admin.Id,
            command.BrandId,
            metricsResult.Value,
            productsResult.Value,
            cancellationToken);

        return ledgerResult.IsFailed
            ? Result.Fail<bool>(ledgerResult.Errors)
            : Result.Ok(true);
    }

    private async Task<Result<IReadOnlyCollection<LoyaltyMetricDefinition>>> EnsureMetricsAsync(
        Guid brandId,
        CancellationToken cancellationToken)
    {
        var existing = await _metricRepository.GetByBrandAsync(brandId, cancellationToken);
        var metrics = existing
            .Where(metric => metric.IsActive)
            .OrderBy(metric => metric.Name)
            .Take(3)
            .ToList();

        while (metrics.Count < 3)
        {
            var template = PickMetricTemplate(metrics.Select(metric => metric.Name).ToHashSet(StringComparer.OrdinalIgnoreCase));
            var metricResult = LoyaltyMetricDefinition.Create(brandId, template.Name, template.RedemptionAmount);
            if (metricResult.IsFailed)
                return Result.Fail(metricResult.Errors);

            metrics.Add(metricResult.Value);
            _metricRepository.Add(metricResult.Value);
        }

        await _metricRepository.SaveAsync(cancellationToken);
        return Result.Ok<IReadOnlyCollection<LoyaltyMetricDefinition>>(metrics);
    }

    private async Task<Result<IReadOnlyCollection<CoinProduct>>> EnsureProductsAsync(
        Guid brandId,
        CancellationToken cancellationToken)
    {
        var existing = await _coinProductRepository.GetByBrandAsync(brandId, cancellationToken);
        var products = existing
            .Where(product => product.IsActive)
            .OrderBy(product => product.Price)
            .Take(3)
            .ToList();

        while (products.Count < 3)
        {
            var template = PickProductTemplate(products.Select(product => product.Name).ToHashSet(StringComparer.OrdinalIgnoreCase));
            var productResult = CoinProduct.Create(brandId, template.Name, template.Price);
            if (productResult.IsFailed)
                return Result.Fail(productResult.Errors);

            products.Add(productResult.Value);
            _coinProductRepository.Add(productResult.Value);
        }

        if (products.Max(product => product.Price) <= products.Min(product => product.Price))
        {
            var productResult = CoinProduct.Create(
                brandId,
                "Подарочный набор",
                products.Min(product => product.Price) + 12);
            if (productResult.IsFailed)
                return Result.Fail(productResult.Errors);

            products.Add(productResult.Value);
            _coinProductRepository.Add(productResult.Value);
        }

        await _coinProductRepository.SaveAsync(cancellationToken);
        return Result.Ok<IReadOnlyCollection<CoinProduct>>(products);
    }

    private async Task<Result> CreateLedgerAsync(
        Guid customerUserId,
        Guid actorUserId,
        Guid brandId,
        IReadOnlyCollection<LoyaltyMetricDefinition> metrics,
        IReadOnlyCollection<CoinProduct> products,
        CancellationToken cancellationToken)
    {
        var orderedProducts = products
            .OrderBy(product => product.Price)
            .ToArray();
        var affordableProduct = orderedProducts[0];
        var expensiveProduct = orderedProducts[^1];
        var targetCoinBalance = Math.Max(
            affordableProduct.Price,
            expensiveProduct.Price - Random.Shared.Next(1, Math.Min(6, expensiveProduct.Price)));
        if (targetCoinBalance >= expensiveProduct.Price)
            targetCoinBalance = expensiveProduct.Price - 1;

        var bonusAmount = Random.Shared.Next(3, 9);
        var startCoinsAmount = targetCoinBalance + affordableProduct.Price - bonusAmount;
        if (startCoinsAmount <= affordableProduct.Price)
            startCoinsAmount = affordableProduct.Price + 1;

        var issueCoinsResult = await _coinLedgerService.IssueAsync(
            customerUserId,
            actorUserId,
            brandId,
            startCoinsAmount,
            Pick(DemoDataCatalog.CoinIssueComments),
            cancellationToken);
        if (issueCoinsResult.IsFailed)
            return Result.Fail(issueCoinsResult.Errors);

        var redeemCoinsResult = await _coinLedgerService.RedeemAsync(
            customerUserId,
            actorUserId,
            brandId,
            affordableProduct.Price,
            affordableProduct.Name,
            cancellationToken);
        if (redeemCoinsResult.IsFailed)
            return Result.Fail(redeemCoinsResult.Errors);

        var bonusCoinsResult = await _coinLedgerService.IssueAsync(
            customerUserId,
            actorUserId,
            brandId,
            bonusAmount,
            Pick(DemoDataCatalog.CoinBonusComments),
            cancellationToken);
        if (bonusCoinsResult.IsFailed)
            return Result.Fail(bonusCoinsResult.Errors);

        var selectedMetrics = metrics
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Min(metrics.Count, 3))
            .ToArray();

        var availableMetricResult = await CreateMetricBalanceAsync(
            customerUserId,
            actorUserId,
            brandId,
            selectedMetrics[0],
            targetBalance: selectedMetrics[0].RedemptionAmount + Random.Shared.Next(1, 4),
            cancellationToken);
        if (availableMetricResult.IsFailed)
            return Result.Fail(availableMetricResult.Errors);

        var unavailableMetricTarget = Math.Max(1, selectedMetrics[1].RedemptionAmount - Random.Shared.Next(1, selectedMetrics[1].RedemptionAmount + 1));
        var unavailableMetricResult = await CreateMetricBalanceAsync(
            customerUserId,
            actorUserId,
            brandId,
            selectedMetrics[1],
            targetBalance: unavailableMetricTarget,
            cancellationToken);
        if (unavailableMetricResult.IsFailed)
            return Result.Fail(unavailableMetricResult.Errors);

        var usedRewardMetricResult = await CreateUsedRewardMetricBalanceAsync(
            customerUserId,
            actorUserId,
            brandId,
            selectedMetrics[2],
            cancellationToken);
        if (usedRewardMetricResult.IsFailed)
            return Result.Fail(usedRewardMetricResult.Errors);

        return Result.Ok();
    }

    private async Task<Result> CreateMetricBalanceAsync(
        Guid customerUserId,
        Guid actorUserId,
        Guid brandId,
        LoyaltyMetricDefinition metric,
        int targetBalance,
        CancellationToken cancellationToken)
    {
        var issueMetricResult = await _metricLedgerService.IssueAsync(
            customerUserId,
            actorUserId,
            brandId,
            metric.Id,
            targetBalance,
            Pick(DemoDataCatalog.MetricIssueComments),
            cancellationToken);

        return issueMetricResult.IsFailed
            ? Result.Fail(issueMetricResult.Errors)
            : Result.Ok();
    }

    private async Task<Result> CreateUsedRewardMetricBalanceAsync(
        Guid customerUserId,
        Guid actorUserId,
        Guid brandId,
        LoyaltyMetricDefinition metric,
        CancellationToken cancellationToken)
    {
        var issueAmount = metric.RedemptionAmount + Random.Shared.Next(2, 6);
        var issueMetricResult = await _metricLedgerService.IssueAsync(
            customerUserId,
            actorUserId,
            brandId,
            metric.Id,
            issueAmount,
            Pick(DemoDataCatalog.MetricIssueComments),
            cancellationToken);
        if (issueMetricResult.IsFailed)
            return Result.Fail(issueMetricResult.Errors);

        var redeemMetricResult = await _metricLedgerService.RedeemAsync(
            customerUserId,
            actorUserId,
            brandId,
            metric.Id,
            metric.RedemptionAmount,
            Pick(DemoDataCatalog.MetricRedeemComments),
            cancellationToken);
        if (redeemMetricResult.IsFailed)
            return Result.Fail(redeemMetricResult.Errors);

        return Result.Ok();
    }

    private static DemoMetricTemplate PickMetricTemplate(IReadOnlySet<string>? excludedNames = null)
    {
        var templates = DemoDataCatalog.BrandTemplates
            .SelectMany(template => template.Metrics)
            .Where(template => excludedNames?.Contains(template.Name) != true)
            .OrderBy(_ => Random.Shared.Next())
            .ToArray();

        return templates.First();
    }

    private static DemoProductTemplate PickProductTemplate(IReadOnlySet<string>? excludedNames = null)
    {
        var templates = DemoDataCatalog.BrandTemplates
            .SelectMany(template => template.Products)
            .Where(template => excludedNames?.Contains(template.Name) != true)
            .OrderBy(_ => Random.Shared.Next())
            .ToArray();

        return templates.First();
    }

    private static string Pick(IReadOnlyCollection<string> items)
    {
        return items.ElementAt(Random.Shared.Next(items.Count));
    }

    private async Task<User?> GetAdminUserAsync(AdminActor admin, CancellationToken cancellationToken)
    {
        if (admin.UserId is { } userId)
            return await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (admin.TelegramUserId is { } telegramUserId)
        {
            return await _userRepository.GetByIdentityAsync(
                IdentityType.Telegram,
                telegramUserId.ToString(),
                cancellationToken);
        }

        return null;
    }
}
