using Microsoft.EntityFrameworkCore;
using StampService.Contracts.DTOs.Coins;
using StampService.Contracts.DTOs.Metrics;
using StampService.Infrastructure;

namespace StampService.TelegramBot.Common.Notifications;

public sealed class CustomerNotificationApplicationAdapter
    : StampService.Application.CustomerNotifications.ICustomerNotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly ICustomerNotificationService _customerNotificationService;

    public CustomerNotificationApplicationAdapter(
        AppDbContext dbContext,
        ICustomerNotificationService customerNotificationService)
    {
        _dbContext = dbContext;
        _customerNotificationService = customerNotificationService;
    }

    public async Task NotifyCoinsIssuedAsync(
        CoinOperationResponse operation,
        CancellationToken cancellationToken)
    {
        var brandName = await _dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.Id == operation.BrandId)
            .Select(brand => brand.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "бренд";

        await _customerNotificationService.NotifyCoinsIssuedAsync(
            operation,
            brandName,
            cancellationToken);
    }

    public async Task NotifyMetricIssuedAsync(
        IssueMetricResponse operation,
        CancellationToken cancellationToken)
    {
        var details = await _dbContext.LoyaltyMetricDefinitions
            .AsNoTracking()
            .Where(metric => metric.Id == operation.MetricDefinitionId)
            .Join(
                _dbContext.Brands.AsNoTracking(),
                metric => metric.BrandId,
                brand => brand.Id,
                (metric, brand) => new
                {
                    BrandName = brand.Name,
                    MetricName = metric.Name
                })
            .FirstOrDefaultAsync(cancellationToken);

        await _customerNotificationService.NotifyMetricIssuedAsync(
            operation,
            details?.BrandName ?? "бренд",
            details?.MetricName ?? "метрика",
            cancellationToken);
    }

    public async Task NotifyCoinsRedeemedAsync(
        CoinOperationResponse operation,
        string comment,
        CancellationToken cancellationToken)
    {
        var brandName = await GetBrandNameAsync(operation.BrandId, cancellationToken);

        await _customerNotificationService.NotifyCoinsRedeemedAsync(
            operation,
            brandName,
            comment,
            cancellationToken);
    }

    public async Task NotifyCoinProductPurchasedAsync(
        CoinOperationResponse operation,
        string productName,
        CancellationToken cancellationToken)
    {
        var brandName = await GetBrandNameAsync(operation.BrandId, cancellationToken);

        await _customerNotificationService.NotifyCoinProductPurchasedAsync(
            operation,
            brandName,
            productName,
            cancellationToken);
    }

    public async Task NotifyMetricRedeemedAsync(
        RedeemMetricResponse operation,
        CancellationToken cancellationToken)
    {
        var details = await GetMetricDetailsAsync(operation.MetricDefinitionId, cancellationToken);

        await _customerNotificationService.NotifyMetricRedeemedAsync(
            operation,
            details?.BrandName ?? "бренд",
            details?.MetricName ?? "метрика",
            cancellationToken);
    }

    private async Task<string> GetBrandNameAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return await _dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.Id == brandId)
            .Select(brand => brand.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "бренд";
    }

    private async Task<MetricDetails?> GetMetricDetailsAsync(
        Guid metricDefinitionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.LoyaltyMetricDefinitions
            .AsNoTracking()
            .Where(metric => metric.Id == metricDefinitionId)
            .Join(
                _dbContext.Brands.AsNoTracking(),
                metric => metric.BrandId,
                brand => brand.Id,
                (metric, brand) => new MetricDetails(brand.Name, metric.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private sealed record MetricDetails(string BrandName, string MetricName);
}
