using StampService.Application.Abstractions;

namespace StampService.Application.Coins.Commands.RedeemCoins;

public record RedeemCoinsCommand(
    Guid BrandId,
    Guid RequestUserId,
    string RedemptionCode,
    int Amount,
    string Comment) : ICommand;
