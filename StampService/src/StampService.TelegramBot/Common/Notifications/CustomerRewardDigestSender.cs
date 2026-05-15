using System.Net;
using Microsoft.EntityFrameworkCore;
using StampService.Application.Abstractions;
using StampService.Application.CustomerNotifications;
using StampService.Application.CustomerNotifications.Queries.GetCustomerRewardDigest;
using StampService.Domain.CustomerNotifications;
using StampService.Domain.User;
using StampService.Infrastructure;
using TelegramBotFlow.Core.Messaging;
using TelegramBotFlow.Core.Sessions;

namespace StampService.TelegramBot.Common.Notifications;

public sealed class CustomerRewardDigestSender
{
    private readonly AppDbContext _dbContext;
    private readonly ICustomerDigestStateRepository _stateRepository;
    private readonly IRewardDigestSettingsRepository _settingsRepository;
    private readonly IQueryHandler<CustomerRewardDigest, GetCustomerRewardDigestQuery> _digestHandler;
    private readonly IBotNotifier _botNotifier;
    private readonly ISessionStore _sessionStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CustomerRewardDigestSender> _logger;

    public CustomerRewardDigestSender(
        AppDbContext dbContext,
        ICustomerDigestStateRepository stateRepository,
        IRewardDigestSettingsRepository settingsRepository,
        IQueryHandler<CustomerRewardDigest, GetCustomerRewardDigestQuery> digestHandler,
        IBotNotifier botNotifier,
        ISessionStore sessionStore,
        TimeProvider timeProvider,
        ILogger<CustomerRewardDigestSender> logger)
    {
        _dbContext = dbContext;
        _stateRepository = stateRepository;
        _settingsRepository = settingsRepository;
        _digestHandler = digestHandler;
        _botNotifier = botNotifier;
        _sessionStore = sessionStore;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SendDueDigestsAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetOrCreateAsync(cancellationToken);
        if (!settings.Enabled)
            return;

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var userIds = await _stateRepository.GetEligibleUserIdsAsync(
            nowUtc,
            TimeSpan.FromMinutes(settings.MessageToUserIntervalMinutes),
            settings.BatchSize,
            cancellationToken);

        foreach (var userId in userIds)
            await TrySendDigestAsync(userId, settings, nowUtc, cancellationToken);
    }

    private async Task TrySendDigestAsync(
        Guid userId,
        RewardDigestSettings settings,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var state = await _stateRepository.GetByUserIdAsync(userId, cancellationToken);
        var interval = TimeSpan.FromMinutes(settings.MessageToUserIntervalMinutes);
        if (state is null || !state.CanSendDigest(nowUtc, interval))
            return;

        var digestResult = await _digestHandler.Handle(
            new GetCustomerRewardDigestQuery(
                userId,
                settings.MaxBrandsPerMessage,
                settings.MaxRewardsPerBrand),
            cancellationToken);

        if (digestResult.IsFailed || digestResult.Value.TotalRewardCount == 0)
            return;

        var chatId = await GetTelegramChatIdAsync(userId, cancellationToken);
        if (chatId is null)
            return;

        try
        {
            await _botNotifier.SendTextAsync(chatId.Value, BuildMessage(digestResult.Value), ct: cancellationToken);
            await ForceNextScreenToNewMessageAsync(chatId.Value, cancellationToken);

            state.MarkDigestSent(nowUtc);
            await _stateRepository.SaveAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send reward digest to Telegram chat {ChatId} for user {UserId}",
                chatId.Value,
                userId);
        }
    }

    private static string BuildMessage(CustomerRewardDigest digest)
    {
        var parts = new List<string> { "🎁 Вам доступны награды:" };

        foreach (var brand in digest.Brands)
        {
            parts.Add(
                $"<b>{Html(brand.BrandName)}</b>\n" +
                string.Join("\n", brand.Rewards.Select(reward =>
                    $"— {Html(reward.RewardName)} за {reward.Price} {Html(reward.UnitName)}")));
        }

        if (digest.HiddenRewardCount > 0)
            parts.Add($"И ещё {digest.HiddenRewardCount} наград — откройте кошелёк, чтобы посмотреть все.");

        parts.Add("Откройте кошелёк, чтобы воспользоваться.");

        return string.Join("\n\n", parts);
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

        return long.TryParse(identityKey, out var chatId)
            ? chatId
            : null;
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
