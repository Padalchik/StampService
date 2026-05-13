using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.CoinProducts;
using StampService.Domain.Access;

namespace StampService.Application.CoinProducts.Commands.UpdateCoinProduct;

public class UpdateCoinProductHandler : ICommandHandler<CoinProductResponse, UpdateCoinProductCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ICoinProductRepository _productRepository;

    public UpdateCoinProductHandler(
        IBrandAccessService brandAccessService,
        ICoinProductRepository productRepository)
    {
        _brandAccessService = brandAccessService;
        _productRepository = productRepository;
    }

    public async Task<Result<CoinProductResponse>> Handle(
        UpdateCoinProductCommand command,
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

        var updateResult = product.UpdateDetails(command.Request.Name, command.Request.Price);
        if (updateResult.IsFailed)
            return Result.Fail(updateResult.Errors);

        await _productRepository.SaveAsync(cancellationToken);

        return Result.Ok(CoinProductMapping.ToResponse(product));
    }
}
