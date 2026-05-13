using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.CoinProducts.Queries.GetCoinProductDetails;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.CoinProducts;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.CoinProducts.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Screens;

public sealed class CoinProductDetailsScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<CoinProductResponse, GetCoinProductDetailsQuery> _productHandler;

    public CoinProductDetailsScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<CoinProductResponse, GetCoinProductDetailsQuery> productHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _productHandler = productHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var productId = ctx.Session?.Data.Get<Guid>(CoinProductSessionKeys.SelectedProductId) ?? Guid.Empty;
        if (productId == Guid.Empty)
            return new ScreenView("Товар не выбран.").BackButton();

        var userResult = await EnsureUserAsync(ctx);
        if (userResult.IsFailed)
            return new ScreenView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}").BackButton();

        var productResult = await _productHandler.Handle(
            new GetCoinProductDetailsQuery(userResult.Value.UserId, productId),
            ctx.CancellationToken);

        if (productResult.IsFailed)
            return new ScreenView($"Не удалось открыть товар: {BotErrorFormatter.Format(productResult.Errors)}").BackButton();

        var product = productResult.Value;
        StoreProduct(ctx, product);

        var status = product.IsActive ? "активен" : "выключен";
        return new ScreenView(
            $"<b>{Html(product.Name)}</b>\n\n" +
            $"Цена: {product.Price} монеток\n" +
            $"Статус: {status}\n" +
            $"Создан: {product.CreatedAt:dd.MM.yyyy HH:mm} UTC")
            .Button<StartEditCoinProductAction>("Редактировать")
            .Row()
            .Button<StartDeleteCoinProductAction>("Удалить")
            .Row()
            .NavigateButton<CoinProductsListScreen>("К товарам")
            .BackButton();
    }

    private static void StoreProduct(UpdateContext ctx, CoinProductResponse product)
    {
        ctx.Session?.Data.Set(CoinProductSessionKeys.SelectedProductId, product.Id);
        ctx.Session?.Data.Set(CoinProductSessionKeys.SelectedProductName, product.Name);
        ctx.Session?.Data.Set(CoinProductSessionKeys.SelectedProductPrice, product.Price);
    }

    private async Task<FluentResults.Result<EnsureTelegramUserResponse>> EnsureUserAsync(UpdateContext ctx)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        return await _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(ctx.UserId, from?.FirstName, from?.LastName, from?.Username),
            ctx.CancellationToken);
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
