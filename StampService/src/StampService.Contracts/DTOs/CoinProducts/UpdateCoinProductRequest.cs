namespace StampService.Contracts.DTOs.CoinProducts;

public record UpdateCoinProductRequest(
    string Name,
    int Price);
