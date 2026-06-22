using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Brand;

public class BrandWelcomeMetricReward
{
    public Guid MetricDefinitionId { get; private set; }
    public int Amount { get; private set; }

    private BrandWelcomeMetricReward(Guid metricDefinitionId, int amount)
    {
        MetricDefinitionId = metricDefinitionId;
        Amount = amount;
    }

    protected BrandWelcomeMetricReward()
    {
    }

    public static Result<BrandWelcomeMetricReward> Create(Guid metricDefinitionId, int amount)
    {
        if (metricDefinitionId == Guid.Empty)
            return Result.Fail(DomainError.Validation(
                "brand.welcome_metric_id_empty",
                "Welcome metric id cannot be empty",
                nameof(metricDefinitionId)));

        if (amount <= 0)
            return Result.Fail(DomainError.Validation(
                "brand.welcome_metric_amount_invalid",
                "Welcome metric amount must be positive",
                nameof(amount)));

        return Result.Ok(new BrandWelcomeMetricReward(metricDefinitionId, amount));
    }
}
