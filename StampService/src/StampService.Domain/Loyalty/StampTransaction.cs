using StampService.Domain.Shared;

namespace StampService.Domain.Loyalty;

public class StampTransaction : BaseEntity
{
    public Guid MetricBalanceId { get; private set; }
    public MetricBalance MetricBalance { get; private set; } = null!;
    public Guid MetricDefinitionId { get; private set; }
    public LoyaltyMetricDefinition MetricDefinition { get; private set; } = null!;
    public int Amount { get; private set; }
    public string Comment { get; private set; }

    private StampTransaction(Guid accountBalanceId, Guid metricDefinitionId, int amount, string comment)
    {
        MetricBalanceId = accountBalanceId;
        MetricDefinitionId = metricDefinitionId;
        Amount = amount;
        Comment = comment;
    }

    // EF Core
    protected StampTransaction()
    {
    }

    public static StampTransaction Create(Guid accountBalanceId, Guid metricDefinitionId, int amount, string comment)
    {
        return new StampTransaction(accountBalanceId, metricDefinitionId, amount, comment);
    }
}
