using StampService.Domain.Coins;

namespace StampService.Application.Coins;

public record CoinLedgerOperation(
    CoinWallet Wallet,
    CoinTransaction Transaction,
    int BalanceBefore,
    int BalanceAfter);
