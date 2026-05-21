using System.Net;
using Microsoft.Extensions.Options;
using StampService.Application.Administration;
using TelegramBotFlow.Core.Messaging;

namespace StampService.TelegramBot.Common.Notifications;

public sealed class BotStartupNotificationHostedService : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IBotNotifier _botNotifier;
    private readonly IOptions<AdminOptions> _adminOptions;
    private readonly IOptions<BotStartupNotificationOptions> _notificationOptions;
    private readonly ILogger<BotStartupNotificationHostedService> _logger;

    public BotStartupNotificationHostedService(
        IHostApplicationLifetime applicationLifetime,
        IBotNotifier botNotifier,
        IOptions<AdminOptions> adminOptions,
        IOptions<BotStartupNotificationOptions> notificationOptions,
        ILogger<BotStartupNotificationHostedService> logger)
    {
        _applicationLifetime = applicationLifetime;
        _botNotifier = botNotifier;
        _adminOptions = adminOptions;
        _notificationOptions = notificationOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForApplicationStartedAsync(stoppingToken);

        if (stoppingToken.IsCancellationRequested)
            return;

        var options = _notificationOptions.Value;
        if (!options.Enabled)
            return;

        var adminIds = _adminOptions.Value.TelegramUserIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (adminIds.Length == 0)
        {
            _logger.LogWarning("Bot startup notifications are enabled, but Admin:TelegramUserIds is empty");
            return;
        }

        foreach (var adminId in adminIds)
        {
            await SendStartupMessageAsync(adminId, "Web interface", options.WebInterfaceUrl, stoppingToken);
            await SendStartupMessageAsync(adminId, "Seq", options.SeqUrl, stoppingToken);
        }
    }

    private async Task WaitForApplicationStartedAsync(CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var startedRegistration = _applicationLifetime.ApplicationStarted.Register(
            static state => ((TaskCompletionSource)state!).TrySetResult(),
            completion);
        using var cancellationRegistration = cancellationToken.Register(
            static state => ((TaskCompletionSource)state!).TrySetCanceled(),
            completion);

        await completion.Task;
    }

    private async Task SendStartupMessageAsync(
        long adminId,
        string linkName,
        string url,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning(
                "Bot startup notification URL is not configured for {LinkName}",
                linkName);
            return;
        }

        try
        {
            var safeLinkName = WebUtility.HtmlEncode(linkName);
            var safeUrl = WebUtility.HtmlEncode(url);
            await _botNotifier.SendTextAsync(
                adminId,
                $"Bot started.\n{safeLinkName}:\n<a href=\"{safeUrl}\">{safeUrl}</a>",
                ct: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send bot startup notification {LinkName} to admin {AdminTelegramUserId}",
                linkName,
                adminId);
        }
    }
}
