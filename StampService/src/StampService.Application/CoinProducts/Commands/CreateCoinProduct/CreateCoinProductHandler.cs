using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.CoinProducts;
using StampService.Domain.Access;
using StampService.Domain.Coins;

namespace StampService.Application.CoinProducts.Commands.CreateCoinProduct;

public class CreateCoinProductHandler : ICommandHandler<CoinProductResponse, CreateCoinProductCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly ICoinProductRepository _productRepository;

    public CreateCoinProductHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        ICoinProductRepository productRepository)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<CoinProductResponse>> Handle(
        CreateCoinProductCommand command,
        CancellationToken cancellationToken)
    {
        var brandExists = await _brandRepository.ExistsAsync(command.BrandId, cancellationToken);
        if (!brandExists)
            return Result.Fail(BrandErrors.NotFound());

        var canManage = await _brandAccessService.CanAsync(
            command.RequestUserId,
            command.BrandId,
            PermissionCode.MetricManage,
            cancellationToken);

        if (!canManage)
            return Result.Fail(AccessErrors.Denied());

        var productResult = CoinProduct.Create(
            command.BrandId,
            command.Request.Name,
            command.Request.Price);

        if (productResult.IsFailed)
            return Result.Fail(productResult.Errors);

        _productRepository.Add(productResult.Value);
        await _productRepository.SaveAsync(cancellationToken);

        return Result.Ok(CoinProductMapping.ToResponse(productResult.Value));
    }
}
