using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Coins;

namespace StampService.Application.Coins.Commands.IssueCoins;

public record IssueCoinsByPhoneCommand(
    Guid BrandId,
    Guid RequestUserId,
    IssueCoinsByPhoneRequest Request) : ICommand;
