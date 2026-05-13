using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.CoinProducts;
using StampService.Domain.Access;

namespace StampService.Application.CoinProducts.Commands.DeleteCoinProduct;

public class DeleteCoinProductHandler : ICommandHandler<CoinProductResponse, DeleteCoinProductCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ICoinProductRepository _productRepository;

    public DeleteCoinProductHandler(
        IBrandAccessService brandAccessService,
        ICoinProductRepository productRepository)
    {
        _brandAccessService = brandAccessService;
        _productRepository = productRepository;
    }

    public async Task<Result<CoinProductResponse>> Handle(
        DeleteCoinProductCommand command,
        CancellationToken cancellationToken)
    {
        if (command.RequestUserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        var product = await _productRepository.GetByIdForUpdateAsync(command.ProductId, cancellationToken);
        if (product is null)
            return Result.Fail(CoinProductErrors.NotFound());

        var canManage = await _brandAccessService.CanAsync(
            command.RequestUserId,
            product.BrandId,
            PermissionCode.MetricManage,
            cancellationToken);

        if (!canManage)
            return Result.Fail(AccessErrors.Denied());

        product.Deactivate();
        await _productRepository.SaveAsync(cancellationToken);

        return Result.Ok(CoinProductMapping.ToResponse(product));
    }
}
