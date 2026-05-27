using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StampService.Application.CoinProducts;
using StampService.Application.CustomerNotifications;
using StampService.Contracts.DTOs.Coins;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.User;

namespace StampService.Infrastructure.Services;

public sealed class TelegramCustomerNotificationService : ICustomerNotificationService
{
    private const int MaxReachedProducts = 3;

    private readonly AppDbContext _dbContext;
    private readonly ICoinProductRepository _coinProductRepository;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramCustomerNotificationService> _logger;

    public TelegramCustomerNotificationService(
        AppDbContext dbContext,
        ICoinProductRepository coinProductRepository,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TelegramCustomerNotificationService> logger)
    {
        _dbContext = dbContext;
        _coinProductRepository = coinProductRepository;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task NotifyCoinsIssuedAsync(
        CoinOperationResponse operation,
        CancellationToken cancellationToken)
    {
        var brand = await _dbContext.Brands
            .AsNoTracking()
            .Where(item => item.Id == operation.BrandId)
            .Select(item => new
            {
                item.Name,
                item.IsCoinProductRedemptionEnabled
            })
            .FirstOrDefaultAsync(cancellationToken);

        var brandName = brand?.Name ?? "бренд";
        var previousBalance = operation.BalanceValue - operation.Amount;
        var reachedProducts = brand?.IsCoinProductRedemptionEnabled == true
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

    public async Task NotifyMetricIssuedAsync(
        IssueMetricResponse operation,
        CancellationToken cancellationToken)
    {
        var details = await _dbContext.LoyaltyMetricDefinitions
            .AsNoTracking()
            .Where(metric => metric.Id == operation.MetricDefinitionId)
            .Join(
                _dbContext.Brands.AsNoTracking(),
                metric => metric.BrandId,
                brand => brand.Id,
                (metric, brand) => new
                {
                    BrandName = brand.Name,
                    MetricName = metric.Name
                })
            .FirstOrDefaultAsync(cancellationToken);

        var brandName = details?.BrandName ?? "бренд";
        var metricName = details?.MetricName ?? "метрика";
        var text =
            $"<b>{Html(brandName)}</b>\n\n" +
            $"Вам начислили {operation.Amount} {Html(metricName)}.\n" +
            $"Баланс: {operation.BalanceValue}";

        await SendToUserAsync(operation.UserId, text, cancellationToken);
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
        var botToken = _configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogWarning("Customer notification was skipped because Telegram:BotToken is not configured");
            return;
        }

        var chatId = await GetTelegramChatIdAsync(userId, cancellationToken);
        if (chatId is null)
            return;

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{botToken}/sendMessage",
                new SendMessageRequest(chatId.Value, text, "HTML"),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Telegram customer notification failed with status {StatusCode} for user {UserId}",
                    (int)response.StatusCode,
                    userId);
            }
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

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("chat_id")] long ChatId,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("parse_mode")] string ParseMode);
}
