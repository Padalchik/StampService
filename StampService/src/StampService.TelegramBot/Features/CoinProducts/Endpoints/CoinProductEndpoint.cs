using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.CoinProducts.Commands.CreateCoinProduct;
using StampService.Application.CoinProducts.Commands.DeleteCoinProduct;
using StampService.Application.CoinProducts.Commands.PurchaseCoinProduct;
using StampService.Application.CoinProducts.Commands.UpdateCoinProduct;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.CoinProducts;
using StampService.Contracts.DTOs.Coins;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Common.Notifications;
using StampService.TelegramBot.Common.Routing;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.CoinProducts.Actions;
using StampService.TelegramBot.Features.CoinProducts.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.CoinProducts.Endpoints;

public sealed class CoinProductEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<OpenCoinProductDetailsAction, OpenCoinProductDetailsPayload>(OpenDetailsAsync);
        app.MapAction<StartCreateCoinProductAction>(StartCreateAsync);
        app.MapInput<EnterCreateCoinProductNameAction>(EnterCreateNameAsync);
        app.MapInput<EnterCreateCoinProductPriceAction>(EnterCreatePriceAsync);
        app.MapAction<ConfirmCreateCoinProductAction>(ConfirmCreateAsync);
        app.MapAction<CancelCreateCoinProductAction>(CancelCreateAsync);
        app.MapAction<StartEditCoinProductAction>(StartEditAsync);
        app.MapInput<EnterEditCoinProductNameAction>(EnterEditNameAsync);
        app.MapAction<KeepEditCoinProductNameAction>(KeepEditNameAsync);
        app.MapInput<EnterEditCoinProductPriceAction>(EnterEditPriceAsync);
        app.MapAction<KeepEditCoinProductPriceAction>(KeepEditPriceAsync);
        app.MapAction<ConfirmEditCoinProductAction>(ConfirmEditAsync);
        app.MapAction<CancelEditCoinProductAction>(CancelEditAsync);
        app.MapAction<StartDeleteCoinProductAction>(StartDeleteAsync);
        app.MapAction<ConfirmDeleteCoinProductAction>(ConfirmDeleteAsync);
        app.MapAction<CancelDeleteCoinProductAction>(CancelDeleteAsync);
        app.MapAction<StartPurchaseCoinProductAction>(StartPurchaseAsync);
        app.MapInput<EnterPurchaseCoinProductCodeAction>(EnterPurchaseCodeAsync);
        app.MapAction<SelectPurchaseCoinProductAction, SelectPurchaseCoinProductPayload>(SelectPurchaseProductAsync);
        app.MapAction<PurchaseCoinProductAction, PurchaseCoinProductPayload>(PurchaseAsync);
        app.MapAction<CancelPurchaseCoinProductAction>(CancelPurchaseAsync);
    }

    private static Task<IEndpointResult> OpenDetailsAsync(
        UpdateContext ctx,
        OpenCoinProductDetailsPayload payload)
    {
        ctx.Session?.Data.Set(CoinProductSessionKeys.SelectedProductId, payload.ProductId);
        return Task.FromResult(BotResults.NavigateTo<CoinProductDetailsScreen>());
    }

    private static Task<IEndpointResult> StartCreateAsync(UpdateContext ctx)
    {
        ClearCreateSession(ctx);
        return Task.FromResult(BotResults.NavigateTo<CreateCoinProductNameScreen>());
    }

    private static Task<IEndpointResult> EnterCreateNameAsync(UpdateContext ctx)
    {
        var name = ctx.MessageText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return Retry<CreateCoinProductNameScreen, EnterCreateCoinProductNameAction>("Название обязательно.");

        ctx.Session?.Data.Set(CoinProductSessionKeys.CreateName, name);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<CreateCoinProductPriceScreen>()));
    }

    private static Task<IEndpointResult> EnterCreatePriceAsync(UpdateContext ctx)
    {
        if (!int.TryParse(ctx.MessageText, out var price) || price <= 0)
            return Retry<CreateCoinProductPriceScreen, EnterCreateCoinProductPriceAction>("Цена должна быть положительным числом.");

        ctx.Session?.Data.Set(CoinProductSessionKeys.CreatePrice, price);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<CreateCoinProductConfirmScreen>()));
    }

    private static async Task<IEndpointResult> ConfirmCreateAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<CoinProductResponse, CreateCoinProductCommand> createHandler)
    {
        var brandId = GetBrandId(ctx);
        var name = ctx.Session?.Data.GetString(CoinProductSessionKeys.CreateName) ?? string.Empty;
        var price = ctx.Session?.Data.Get<int>(CoinProductSessionKeys.CreatePrice) ?? 0;

        if (brandId == Guid.Empty || string.IsNullOrWhiteSpace(name) || price <= 0)
            return BotResults.ShowView(new ScreenView("Сценарий создания устарел. Начните заново.").BackButton());

        var userResult = await EnsureUserAsync(ctx, ensureUserHandler);
        if (userResult.IsFailed)
            return ErrorView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}");

        var result = await createHandler.Handle(
            new CreateCoinProductCommand(
                brandId,
                userResult.Value.UserId,
                new CreateCoinProductRequest(name, price)),
            ctx.CancellationToken);

        if (result.IsFailed)
            return ErrorView($"Не удалось создать товар: {BotErrorFormatter.Format(result.Errors)}");

        ClearCreateSession(ctx);
        StoreProduct(ctx, result.Value);
        return BotResults.NavigateTo<CoinProductsListScreen>();
    }

    private static Task<IEndpointResult> CancelCreateAsync(UpdateContext ctx)
    {
        ClearCreateSession(ctx);
        return Task.FromResult(BotResults.NavigateTo<CoinProductsListScreen>());
    }

    private static Task<IEndpointResult> StartEditAsync(UpdateContext ctx)
    {
        var productId = ctx.Session?.Data.Get<Guid>(CoinProductSessionKeys.SelectedProductId) ?? Guid.Empty;
        if (productId == Guid.Empty)
            return Task.FromResult(BotResults.ShowView(new ScreenView("Товар не выбран.").BackButton()));

        ctx.Session?.Data.Set(
            CoinProductSessionKeys.EditName,
            ctx.Session.Data.GetString(CoinProductSessionKeys.SelectedProductName) ?? string.Empty);
        ctx.Session?.Data.Set(
            CoinProductSessionKeys.EditPrice,
            ctx.Session.Data.Get<int>(CoinProductSessionKeys.SelectedProductPrice));

        return Task.FromResult(BotResults.NavigateTo<EditCoinProductNameScreen>());
    }

    private static Task<IEndpointResult> EnterEditNameAsync(UpdateContext ctx)
    {
        var name = ctx.MessageText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return Retry<EditCoinProductNameScreen, EnterEditCoinProductNameAction>("Название обязательно.");

        ctx.Session?.Data.Set(CoinProductSessionKeys.EditName, name);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<EditCoinProductPriceScreen>()));
    }

    private static Task<IEndpointResult> KeepEditNameAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.GetString(CoinProductSessionKeys.SelectedProductName) ?? string.Empty;
        ctx.Session?.Data.Set(CoinProductSessionKeys.EditName, current);
        return Task.FromResult(BotResults.NavigateTo<EditCoinProductPriceScreen>());
    }

    private static Task<IEndpointResult> EnterEditPriceAsync(UpdateContext ctx)
    {
        if (!int.TryParse(ctx.MessageText, out var price) || price <= 0)
            return Retry<EditCoinProductPriceScreen, EnterEditCoinProductPriceAction>("Цена должна быть положительным числом.");

        ctx.Session?.Data.Set(CoinProductSessionKeys.EditPrice, price);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<EditCoinProductConfirmScreen>()));
    }

    private static Task<IEndpointResult> KeepEditPriceAsync(UpdateContext ctx)
    {
        var current = ctx.Session?.Data.Get<int>(CoinProductSessionKeys.SelectedProductPrice) ?? 0;
        ctx.Session?.Data.Set(CoinProductSessionKeys.EditPrice, current);
        return Task.FromResult(BotResults.NavigateTo<EditCoinProductConfirmScreen>());
    }

    private static async Task<IEndpointResult> ConfirmEditAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<CoinProductResponse, UpdateCoinProductCommand> updateHandler)
    {
        var productId = ctx.Session?.Data.Get<Guid>(CoinProductSessionKeys.SelectedProductId) ?? Guid.Empty;
        var name = ctx.Session?.Data.GetString(CoinProductSessionKeys.EditName) ?? string.Empty;
        var price = ctx.Session?.Data.Get<int>(CoinProductSessionKeys.EditPrice) ?? 0;

        if (productId == Guid.Empty || string.IsNullOrWhiteSpace(name) || price <= 0)
            return BotResults.ShowView(new ScreenView("Сценарий редактирования устарел. Начните заново.").BackButton());

        var userResult = await EnsureUserAsync(ctx, ensureUserHandler);
        if (userResult.IsFailed)
            return ErrorView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}");

        var result = await updateHandler.Handle(
            new UpdateCoinProductCommand(
                productId,
                userResult.Value.UserId,
                new UpdateCoinProductRequest(name, price)),
            ctx.CancellationToken);

        if (result.IsFailed)
            return ErrorView($"Не удалось сохранить товар: {BotErrorFormatter.Format(result.Errors)}");

        ClearEditSession(ctx);
        StoreProduct(ctx, result.Value);
        return BotResults.NavigateTo<CoinProductDetailsScreen>();
    }

    private static Task<IEndpointResult> CancelEditAsync(UpdateContext ctx)
    {
        ClearEditSession(ctx);
        return Task.FromResult(BotResults.NavigateTo<CoinProductDetailsScreen>());
    }

    private static Task<IEndpointResult> StartDeleteAsync(UpdateContext ctx)
    {
        return Task.FromResult(BotResults.NavigateTo<DeleteCoinProductConfirmScreen>());
    }

    private static async Task<IEndpointResult> ConfirmDeleteAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<CoinProductResponse, DeleteCoinProductCommand> deleteHandler)
    {
        var productId = ctx.Session?.Data.Get<Guid>(CoinProductSessionKeys.SelectedProductId) ?? Guid.Empty;
        if (productId == Guid.Empty)
            return BotResults.ShowView(new ScreenView("Товар не выбран.").BackButton());

        var userResult = await EnsureUserAsync(ctx, ensureUserHandler);
        if (userResult.IsFailed)
            return ErrorView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}");

        var result = await deleteHandler.Handle(
            new DeleteCoinProductCommand(productId, userResult.Value.UserId),
            ctx.CancellationToken);

        if (result.IsFailed)
            return ErrorView($"Не удалось удалить товар: {BotErrorFormatter.Format(result.Errors)}");

        return BotResults.NavigateTo<CoinProductsListScreen>();
    }

    private static Task<IEndpointResult> CancelDeleteAsync(UpdateContext ctx)
    {
        return Task.FromResult(BotResults.NavigateTo<CoinProductDetailsScreen>());
    }

    private static Task<IEndpointResult> StartPurchaseAsync(UpdateContext ctx)
    {
        ClearPurchaseSession(ctx);
        return Task.FromResult(BotResults.NavigateTo<PurchaseCoinProductCodeScreen>());
    }

    private static Task<IEndpointResult> EnterPurchaseCodeAsync(UpdateContext ctx)
    {
        var code = ctx.MessageText?.Trim() ?? string.Empty;
        ctx.Session?.Data.Set(CoinProductSessionKeys.PurchaseRedemptionCode, code);
        ClearSelectedPurchaseProduct(ctx);
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.NavigateTo<PurchaseCoinProductSelectScreen>()));
    }

    private static Task<IEndpointResult> SelectPurchaseProductAsync(
        UpdateContext ctx,
        SelectPurchaseCoinProductPayload payload)
    {
        if (!payload.CanPurchase)
            return Task.FromResult(BotResults.NavigateTo<PurchaseCoinProductSelectScreen>());

        ctx.Session?.Data.Set(CoinProductSessionKeys.PurchaseProductId, payload.ProductId);
        ctx.Session?.Data.Set(CoinProductSessionKeys.PurchaseProductName, payload.ProductName);
        ctx.Session?.Data.Set(CoinProductSessionKeys.PurchaseProductPrice, payload.Price);
        ctx.Session?.Data.Set(CoinProductSessionKeys.PurchaseCurrentBalance, payload.CurrentBalance);

        return Task.FromResult(BotResults.NavigateTo<PurchaseCoinProductConfirmScreen>());
    }

    private static async Task<IEndpointResult> PurchaseAsync(
        UpdateContext ctx,
        PurchaseCoinProductPayload payload,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        ICommandHandler<CoinOperationResponse, PurchaseCoinProductCommand> purchaseHandler,
        ICustomerNotificationService customerNotificationService)
    {
        if (!payload.CanPurchase)
            return BotResults.NavigateTo<PurchaseCoinProductSelectScreen>();

        var brandId = GetBrandId(ctx);
        var code = ctx.Session?.Data.GetString(CoinProductSessionKeys.PurchaseRedemptionCode) ?? string.Empty;
        var productId = ctx.Session?.Data.Get<Guid>(CoinProductSessionKeys.PurchaseProductId) ?? payload.ProductId;
        if (brandId == Guid.Empty || string.IsNullOrWhiteSpace(code) || productId == Guid.Empty)
            return BotResults.ShowView(new ScreenView("Сценарий покупки устарел. Начните заново.").BackButton());

        var userResult = await EnsureUserAsync(ctx, ensureUserHandler);
        if (userResult.IsFailed)
            return ErrorView($"Не удалось определить пользователя: {BotErrorFormatter.Format(userResult.Errors)}");

        var result = await purchaseHandler.Handle(
            new PurchaseCoinProductCommand(
                brandId,
                userResult.Value.UserId,
                code,
                productId),
            ctx.CancellationToken);

        if (result.IsFailed)
            return ErrorView($"Не удалось оформить покупку: {BotErrorFormatter.Format(result.Errors)}");

        var brandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey) ?? "бренд";
        var productName = ctx.Session?.Data.GetString(CoinProductSessionKeys.PurchaseProductName) ?? "товар";
        await customerNotificationService.NotifyCoinProductPurchasedAsync(
            result.Value,
            brandName,
            productName,
            ctx.CancellationToken);

        ClearPurchaseSession(ctx);
        return BotResults.ShowView(new ScreenView(
            "<b>Покупка оформлена</b>\n\n" +
            $"Клиент: {Html(result.Value.UserName)} · <code>{Html(result.Value.CustomerCode)}</code>\n" +
            $"Списано: {result.Value.Amount} монеток\n" +
            $"Баланс: {result.Value.BalanceValue}"));
    }

    private static Task<IEndpointResult> CancelPurchaseAsync(UpdateContext ctx)
    {
        ClearPurchaseSession(ctx);
        return Task.FromResult(BotResults.NavigateTo<ClientWorkScreen>());
    }

    private static async Task<FluentResults.Result<EnsureTelegramUserResponse>> EnsureUserAsync(
        UpdateContext ctx,
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        return await ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(ctx.UserId, from?.FirstName, from?.LastName, from?.Username),
            ctx.CancellationToken);
    }

    private static Task<IEndpointResult> Retry<TScreen, TAction>(string message)
        where TScreen : IScreen
        where TAction : IBotAction
    {
        return Task.FromResult(BotInputResults.DeleteInputThen(BotResults.ShowView(new ScreenView(message)
            .AwaitInput<TAction>()
            .BackButton())));
    }

    private static IEndpointResult ErrorView(string message)
    {
        return BotResults.ShowView(new ScreenView(message).BackButton());
    }

    private static Guid GetBrandId(UpdateContext ctx)
    {
        return ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
    }

    private static void StoreProduct(UpdateContext ctx, CoinProductResponse product)
    {
        ctx.Session?.Data.Set(CoinProductSessionKeys.SelectedProductId, product.Id);
        ctx.Session?.Data.Set(CoinProductSessionKeys.SelectedProductName, product.Name);
        ctx.Session?.Data.Set(CoinProductSessionKeys.SelectedProductPrice, product.Price);
    }

    private static void ClearCreateSession(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(CoinProductSessionKeys.CreateName);
        ctx.Session?.Data.Remove(CoinProductSessionKeys.CreatePrice);
    }

    private static void ClearEditSession(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(CoinProductSessionKeys.EditName);
        ctx.Session?.Data.Remove(CoinProductSessionKeys.EditPrice);
    }

    private static void ClearPurchaseSession(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(CoinProductSessionKeys.PurchaseRedemptionCode);
        ctx.Session?.Data.Remove(CoinProductSessionKeys.PurchaseCustomerName);
        ClearSelectedPurchaseProduct(ctx);
    }

    private static void ClearSelectedPurchaseProduct(UpdateContext ctx)
    {
        ctx.Session?.Data.Remove(CoinProductSessionKeys.PurchaseProductId);
        ctx.Session?.Data.Remove(CoinProductSessionKeys.PurchaseProductName);
        ctx.Session?.Data.Remove(CoinProductSessionKeys.PurchaseProductPrice);
        ctx.Session?.Data.Remove(CoinProductSessionKeys.PurchaseCurrentBalance);
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
