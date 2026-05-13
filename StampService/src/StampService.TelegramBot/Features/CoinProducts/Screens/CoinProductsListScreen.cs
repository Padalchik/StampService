using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.CoinProducts.Queries.GetBrandCoinProducts;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.CoinProducts;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.CoinProducts.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Screens;

public sealed class CoinProductsListScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<IReadOnlyCollection<CoinProductResponse>, GetBrandCoinProductsQuery> _productsHandler;

    public CoinProductsListScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<IReadOnlyCollection<CoinProductResponse>, GetBrandCoinProductsQuery> productsHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _productsHandler = productsHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandId = ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        if (brandId == Guid.Empty)
            return new ScreenView("Бренд не выбран.").BackButton();

        var userResult = await EnsureUserAsync(ctx);
        if (userResult.IsFailed)
            return new ScreenView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}").BackButton();

        var productsResult = await _productsHandler.Handle(
            new GetBrandCoinProductsQuery(userResult.Value.UserId, brandId),
            ctx.CancellationToken);

        if (productsResult.IsFailed)
            return new ScreenView($"Не удалось загрузить товары: {BotErrorFormatter.Format(productsResult.Errors)}").BackButton();

        var view = new ScreenView(
            $"<b>Товары за монетки</b>\n{Html(brandName)}\n\n" +
            (productsResult.Value.Count == 0
                ? "Товаров пока нет."
                : "Выберите товар:"));

        foreach (var product in productsResult.Value)
        {
            var status = product.IsActive ? "" : " · выкл.";
            view.Row().Button<OpenCoinProductDetailsAction, OpenCoinProductDetailsPayload>(
                $"{product.Name} · {product.Price} монеток{status}",
                new OpenCoinProductDetailsPayload(product.Id));
        }

        return view.Row()
            .Button<StartCreateCoinProductAction>("➕ Создать новый товар")
            .BackButton();
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
