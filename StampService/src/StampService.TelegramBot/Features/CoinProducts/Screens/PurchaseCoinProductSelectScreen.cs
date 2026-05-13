using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.CoinProducts.Queries.GetCoinProductPurchaseOptions;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.CoinProducts;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.CoinProducts.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Screens;

public sealed class PurchaseCoinProductSelectScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<CoinProductPurchaseOptionsResponse, GetCoinProductPurchaseOptionsQuery> _optionsHandler;

    public PurchaseCoinProductSelectScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<CoinProductPurchaseOptionsResponse, GetCoinProductPurchaseOptionsQuery> optionsHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _optionsHandler = optionsHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandId = ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
        var redemptionCode = ctx.Session?.Data.GetString(CoinProductSessionKeys.PurchaseRedemptionCode) ?? string.Empty;
        if (brandId == Guid.Empty || string.IsNullOrWhiteSpace(redemptionCode))
            return new ScreenView("Сценарий покупки устарел. Начните заново.").BackButton();

        var userResult = await EnsureUserAsync(ctx);
        if (userResult.IsFailed)
            return new ScreenView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}").BackButton();

        var optionsResult = await _optionsHandler.Handle(
            new GetCoinProductPurchaseOptionsQuery(userResult.Value.UserId, brandId, redemptionCode),
            ctx.CancellationToken);

        if (optionsResult.IsFailed)
            return new ScreenView($"Не удалось загрузить товары: {BotErrorFormatter.Format(optionsResult.Errors)}").BackButton();

        var options = optionsResult.Value;
        ctx.Session?.Data.Set(CoinProductSessionKeys.PurchaseCustomerName, options.CustomerName);

        var view = new ScreenView(
            $"<b>Покупка товара</b>\n\n" +
            $"Клиент: {Html(options.CustomerName)}\n" +
            $"Код списания: <code>{Html(options.RedemptionCode)}</code>\n\n" +
            (options.Products.Count == 0
                ? "Активных товаров пока нет."
                : "Выберите товар:"));

        foreach (var product in options.Products)
        {
            var label = product.CanPurchase
                ? $"{product.ProductName} · {product.CurrentBalance}/{product.Price}"
                : $"⛔️ {product.ProductName} · {product.CurrentBalance}/{product.Price}";

            view.Row().Button<SelectPurchaseCoinProductAction, SelectPurchaseCoinProductPayload>(
                label,
                new SelectPurchaseCoinProductPayload(
                    product.ProductId,
                    product.ProductName,
                    product.Price,
                    product.CurrentBalance,
                    product.CanPurchase));
        }

        return view.BackButton();
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
