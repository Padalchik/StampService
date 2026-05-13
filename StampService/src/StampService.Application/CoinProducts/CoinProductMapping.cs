using StampService.Contracts.DTOs.CoinProducts;
using StampService.Domain.Coins;

namespace StampService.Application.CoinProducts;

internal static class CoinProductMapping
{
    public static CoinProductResponse ToResponse(CoinProduct product)
    {
        return new CoinProductResponse(
            product.Id,
            product.BrandId,
            product.Name,
            product.Price,
            product.IsActive,
            product.CreatedAt);
    }
}
