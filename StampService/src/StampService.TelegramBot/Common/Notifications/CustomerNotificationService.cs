using System.Net;
using Microsoft.EntityFrameworkCore;
using StampService.Application.CoinProducts;
using StampService.Contracts.DTOs.Coins;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.User;
using StampService.Infrastructure;
using TelegramBotFlow.Core.Messaging;
using TelegramBotFlow.Core.Sessions;

namespace StampService.TelegramBot.Common.Notifications;

public sealed class CustomerNotificationService : ICustomerNotificationService
{
    private const int MaxReachedProducts = 3;

    private readonly AppDbContext _dbContext;
    private readonly ICoinProductRepository _coinProductRepository;
    private readonly IBotNotifier _botNotifier;
    private readonly ISessionStore _sessionStore;
    private readonly ILogger<CustomerNotificationService> _logger;

    public CustomerNotificationService(
        AppDbContext dbContext,
        ICoinProductRepository coinProductRepository,
        IBotNotifier botNotifier,
        ISessionStore sessionStore,
        ILogger<CustomerNotificationService> logger)
    {
        _dbContext = dbContext;
        _coinProductRepository = coinProductRepository;
        _botNotifier = botNotifier;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public async Task NotifyCoinsIssuedAsync(
        CoinOperationResponse operation,
        string brandName,
        CancellationToken cancellationToken)
    {
        var previousBalance = operation.BalanceValue - operation.Amount;
        var isCoinProductRedemptionEnabled = await _dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.Id == operation.BrandId)
            .Select(brand => brand.IsCoinProductRedemptionEnabled)
            .FirstOrDefaultAsync(cancellationToken);
        var reachedProducts = isCoinProductRedemptionEnabled
            ? await GetNewlyReachedProductsAsync(
                operation.BrandId,
                previousBalance,
                operation.BalanceValue,
                cancellationToken)
            : [];

        var text =
            $"<b>{Html(brandName)}</b>\n\n" +
            $"Вам начислили {operation.Amount} монеток.\n" +
            $"Баланс: {operation.BalanceValue}";

        if (reachedProducts.Count > 0)
        {
            text += "\n\nТеперь можно получить:\n" +
                string.Join("\n", reachedProducts.Select(product =>
                    $"- {Html(product.Name)} за {product.Price} монеток"));
        }

        await SendToUserAsync(operation.UserId, text, cancellationToken);
    }

    public async Task NotifyCoinProductPurchasedAsync(
        CoinOperationResponse operation,
        string brandName,
        string productName,
        CancellationToken cancellationToken)
    {
        var text =
            $"<b>{Html(brandName)}</b>\n\n" +
            $"Покупка оформлена: {Html(productName)}.\n" +
            $"Списано: {operation.Amount} монеток.\n" +
            $"Баланс: {operation.BalanceValue}";

        await SendToUserAsync(operation.UserId, text, cancellationToken);
    }

    public Task NotifyCoinsRedeemedAsync(
        CoinOperationResponse operation,
        string brandName,
        string comment,
        CancellationToken cancellationToken)
    {
        var text =
            $"<b>{Html(brandName)}</b>\n\n" +
            $"Списано: {operation.Amount} монеток.\n" +
            $"Назначение: {Html(comment)}.\n" +
            $"Баланс: {operation.BalanceValue}";

        return SendToUserAsync(operation.UserId, text, cancellationToken);
    }

    public Task NotifyMetricIssuedAsync(
        IssueMetricResponse operation,
        string brandName,
        string metricName,
        CancellationToken cancellationToken)
    {
        var text =
            $"<b>{Html(brandName)}</b>\n\n" +
            $"Вам начислили {operation.Amount} {Html(metricName)}.\n" +
            $"Баланс: {operation.BalanceValue}";

        return SendToUserAsync(operation.UserId, text, cancellationToken);
    }

    public Task NotifyMetricRedeemedAsync(
        RedeemMetricResponse operation,
        string brandName,
        string metricName,
        CancellationToken cancellationToken)
    {
        var text =
            $"<b>{Html(brandName)}</b>\n\n" +
            $"Списано {operation.Amount} {Html(metricName)}.\n" +
            $"Баланс: {operation.BalanceValue}";

        return SendToUserAsync(operation.UserId, text, cancellationToken);
    }

    private async Task<IReadOnlyCollection<ReachedProduct>> GetNewlyReachedProductsAsync(
        Guid brandId,
        int previousBalance,
        int currentBalance,
        CancellationToken cancellationToken)
    {
        if (currentBalance <= previousBalance)
            return [];

        var products = await _coinProductRepository.GetActiveByBrandAsync(brandId, cancellationToken);

        return products
            .Where(product => product.Price > previousBalance && product.Price <= currentBalance)
            .OrderBy(product => product.Price)
            .ThenBy(product => product.Name)
            .Take(MaxReachedProducts)
            .Select(product => new ReachedProduct(product.Name, product.Price))
            .ToArray();
    }

    private async Task SendToUserAsync(
        Guid userId,
        string text,
        CancellationToken cancellationToken)
    {
        var chatId = await GetTelegramChatIdAsync(userId, cancellationToken);
        if (chatId is null)
            return;

        try
        {
            await _botNotifier.SendTextAsync(chatId.Value, text, ct: cancellationToken);
            await ForceNextScreenToNewMessageAsync(chatId.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send customer notification to Telegram chat {ChatId} for user {UserId}",
                chatId.Value,
                userId);
        }
    }

    private async Task ForceNextScreenToNewMessageAsync(
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        var session = await _sessionStore.GetOrCreateAsync(telegramUserId, cancellationToken);
        session.Navigation.ForgetNavMessage();
        await _sessionStore.SaveAsync(session, cancellationToken);
    }

    private async Task<long?> GetTelegramChatIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var identityKey = await _dbContext.UserIdentities
            .AsNoTracking()
            .Where(identity => identity.UserId == userId && identity.Type == IdentityType.Telegram)
            .Select(identity => identity.Key)
            .FirstOrDefaultAsync(cancellationToken);

        if (!long.TryParse(identityKey, out var chatId))
            return null;

        return chatId;
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private sealed record ReachedProduct(string Name, int Price);
}
