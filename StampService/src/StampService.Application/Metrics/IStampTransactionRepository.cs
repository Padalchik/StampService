using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics;

public interface IStampTransactionRepository
{
    void Add(StampTransaction transaction);

    Task SaveAsync(CancellationToken cancellationToken);
}
