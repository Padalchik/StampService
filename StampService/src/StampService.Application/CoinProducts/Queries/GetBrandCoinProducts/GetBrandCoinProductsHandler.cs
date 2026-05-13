using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.CoinProducts;
using StampService.Domain.Access;

namespace StampService.Application.CoinProducts.Queries.GetBrandCoinProducts;

public class GetBrandCoinProductsHandler
    : IQueryHandler<IReadOnlyCollection<CoinProductResponse>, GetBrandCoinProductsQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly ICoinProductRepository _productRepository;

    public GetBrandCoinProductsHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        ICoinProductRepository productRepository)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<IReadOnlyCollection<CoinProductResponse>>> Handle(
        GetBrandCoinProductsQuery query,
        CancellationToken cancellationToken)
    {
        var brandExists = await _brandRepository.ExistsAsync(query.BrandId, cancellationToken);
        if (!brandExists)
            return Result.Fail(BrandErrors.NotFound());

        var canManage = await _brandAccessService.CanAsync(
            query.RequestUserId,
            query.BrandId,
            PermissionCode.MetricManage,
            cancellationToken);

        if (!canManage)
            return Result.Fail(AccessErrors.Denied());

        var products = await _productRepository.GetByBrandAsync(query.BrandId, cancellationToken);
        return Result.Ok<IReadOnlyCollection<CoinProductResponse>>(
            products.Select(CoinProductMapping.ToResponse).ToArray());
    }
}
