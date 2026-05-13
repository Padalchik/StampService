namespace StampService.Contracts.DTOs.CoinProducts;

public record CreateCoinProductRequest(
    string Name,
    int Price);
