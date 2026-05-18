using StampService.Application.Abstractions;

namespace StampService.Application.Wallet.Commands.OpenUserWallet;

public record OpenUserWalletCommand(
    Guid UserId,
    bool ForceRefreshCode = false) : ICommand;
