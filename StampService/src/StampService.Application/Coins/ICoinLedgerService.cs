using FluentResults;

namespace StampService.Application.Coins;

public interface ICoinLedgerService
{
    Task<Result<CoinLedgerOperation>> IssueAsync(
        Guid userId,
        Guid actorUserId,
        Guid brandId,
        int amount,
        string comment,
        CancellationToken cancellationToken);

    Task<Result<CoinLedgerOperation>> RedeemAsync(
        Guid userId,
        Guid actorUserId,
        Guid brandId,
        int amount,
        string comment,
        CancellationToken cancellationToken);
}
