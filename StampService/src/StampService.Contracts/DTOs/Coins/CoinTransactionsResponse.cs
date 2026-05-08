namespace StampService.Contracts.DTOs.Coins;

public record CoinTransactionsResponse(
    Guid BrandId,
    Guid UserId,
    int Skip,
    int Take,
    IReadOnlyCollection<CoinTransactionResponse> Items);
