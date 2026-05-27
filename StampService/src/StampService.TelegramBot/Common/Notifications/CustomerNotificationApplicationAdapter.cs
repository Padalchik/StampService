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
}
