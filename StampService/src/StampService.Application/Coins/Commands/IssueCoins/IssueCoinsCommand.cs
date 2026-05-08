using StampService.Application.Abstractions;

namespace StampService.Application.Coins.Commands.IssueCoins;

public record IssueCoinsCommand(
    Guid BrandId,
    Guid RequestUserId,
    string CustomerCode,
    int Amount,
    string Comment) : ICommand;
