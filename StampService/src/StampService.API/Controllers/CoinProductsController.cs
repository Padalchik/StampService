using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StampService.API.EndpointResults;
using StampService.Application.Abstractions;
using StampService.Application.CoinProducts.Commands.CreateCoinProduct;
using StampService.Application.CoinProducts.Commands.DeleteCoinProduct;
using StampService.Application.CoinProducts.Commands.PurchaseCoinProduct;
using StampService.Application.CoinProducts.Commands.UpdateCoinProduct;
using StampService.Application.CoinProducts.Queries.GetBrandCoinProducts;
using StampService.Application.CoinProducts.Queries.GetCoinProductDetails;
using StampService.Application.CoinProducts.Queries.GetCoinProductPurchaseOptions;
using StampService.Contracts.DTOs.CoinProducts;
using StampService.Contracts.DTOs.Coins;

namespace StampService.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class CoinProductsController : ApiControllerBase
{
    [HttpPost("brands/{brandId:guid}/coin-products")]
    public async Task<EndpointResult<CoinProductResponse>> Create(
        Guid brandId,
        CreateCoinProductRequest request,
        [FromServices] ICommandHandler<CoinProductResponse, CreateCoinProductCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CoinProductResponse>();

        return EndpointResult<CoinProductResponse>.Created(
            await handler.Handle(
                new CreateCoinProductCommand(brandId, userIdResult.Value, request),
                cancellationToken));
    }

    [HttpGet("brands/{brandId:guid}/coin-products")]
    public async Task<EndpointResult<IReadOnlyCollection<CoinProductResponse>>> GetByBrand(
        Guid brandId,
        [FromServices] IQueryHandler<IReadOnlyCollection<CoinProductResponse>, GetBrandCoinProductsQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<IReadOnlyCollection<CoinProductResponse>>();

        return await handler.Handle(
            new GetBrandCoinProductsQuery(userIdResult.Value, brandId),
            cancellationToken);
    }

    [HttpGet("coin-products/{productId:guid}")]
    public async Task<EndpointResult<CoinProductResponse>> GetDetails(
        Guid productId,
        [FromServices] IQueryHandler<CoinProductResponse, GetCoinProductDetailsQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CoinProductResponse>();

        return await handler.Handle(
            new GetCoinProductDetailsQuery(userIdResult.Value, productId),
            cancellationToken);
    }

    [HttpPut("coin-products/{productId:guid}")]
    public async Task<EndpointResult<CoinProductResponse>> Update(
        Guid productId,
        UpdateCoinProductRequest request,
        [FromServices] ICommandHandler<CoinProductResponse, UpdateCoinProductCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CoinProductResponse>();

        return await handler.Handle(
            new UpdateCoinProductCommand(productId, userIdResult.Value, request),
            cancellationToken);
    }

    [HttpDelete("coin-products/{productId:guid}")]
    public async Task<EndpointResult<CoinProductResponse>> Delete(
        Guid productId,
        [FromServices] ICommandHandler<CoinProductResponse, DeleteCoinProductCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CoinProductResponse>();

        return await handler.Handle(
            new DeleteCoinProductCommand(productId, userIdResult.Value),
            cancellationToken);
    }

    [HttpGet("brands/{brandId:guid}/coin-products/purchase-options")]
    public async Task<EndpointResult<CoinProductPurchaseOptionsResponse>> GetPurchaseOptions(
        Guid brandId,
        [FromQuery] string redemptionCode,
        [FromServices] IQueryHandler<CoinProductPurchaseOptionsResponse, GetCoinProductPurchaseOptionsQuery> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CoinProductPurchaseOptionsResponse>();

        return await handler.Handle(
            new GetCoinProductPurchaseOptionsQuery(userIdResult.Value, brandId, redemptionCode),
            cancellationToken);
    }

    [HttpPost("brands/{brandId:guid}/coin-products/{productId:guid}/purchase")]
    public async Task<EndpointResult<CoinOperationResponse>> Purchase(
        Guid brandId,
        Guid productId,
        PurchaseCoinProductRequest request,
        [FromServices] ICommandHandler<CoinOperationResponse, PurchaseCoinProductCommand> handler,
        CancellationToken cancellationToken)
    {
        var userIdResult = GetUserId();
        if (userIdResult.IsFailed)
            return userIdResult.ToResult<CoinOperationResponse>();

        return await handler.Handle(
            new PurchaseCoinProductCommand(
                brandId,
                userIdResult.Value,
                request.RedemptionCode,
                productId),
            cancellationToken);
    }

}
