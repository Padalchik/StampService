using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.CoinProducts;
using StampService.Domain.Access;

namespace StampService.Application.CoinProducts.Queries.GetCoinProductDetails;

public class GetCoinProductDetailsHandler : IQueryHandler<CoinProductResponse, GetCoinProductDetailsQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ICoinProductRepository _productRepository;

    public GetCoinProductDetailsHandler(
        IBrandAccessService brandAccessService,
        ICoinProductRepository productRepository)
    {
        _brandAccessService = brandAccessService;
        _productRepository = productRepository;
    }

    public async Task<Result<CoinProductResponse>> Handle(
        GetCoinProductDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(query.ProductId, cancellationToken);
        if (product is null)
            return Result.Fail(CoinProductErrors.NotFound());

        var canManage = await _brandAccessService.CanAsync(
            query.RequestUserId,
            product.BrandId,
            PermissionCode.MetricManage,
            cancellationToken);

        if (!canManage)
            return Result.Fail(AccessErrors.Denied());

        return Result.Ok(CoinProductMapping.ToResponse(product));
    }
}
