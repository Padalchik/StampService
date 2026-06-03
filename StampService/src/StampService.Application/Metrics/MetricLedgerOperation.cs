using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics;

public record MetricLedgerOperation(
    MetricBalance Balance,
    StampTransaction Transaction,
    int BalanceBefore,
    int BalanceAfter);
