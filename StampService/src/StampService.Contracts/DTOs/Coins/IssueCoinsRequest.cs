namespace StampService.Contracts.DTOs.Coins;

public record IssueCoinsRequest(
    string CustomerCode,
    int Amount,
    string? Comment);
