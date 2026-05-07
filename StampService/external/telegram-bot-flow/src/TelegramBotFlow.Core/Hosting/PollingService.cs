using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace TelegramBotFlow.Core.Hosting;

/// <summary>
/// Фоновый сервис получения Telegram update-ов в режиме polling.
/// Только читает обновления и складывает их в Channel для обработки воркерами.
/// </summary>
internal sealed class PollingService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<PollingService> _logger;
    private readonly BotConfiguration _config;
    private readonly ChannelWriter<Update> _updateWriter;

    /// <summary>
    /// Создаёт polling-сервис с зависимостями обработки update-ов.
    /// </summary>
    public PollingService(
        ITelegramBotClient bot,
        ILogger<PollingService> logger,
        IOptions<BotConfiguration> configOptions,
        ChannelWriter<Update> updateWriter)
    {
        _bot = bot;
        _logger = logger;
        _config = configOptions.Value;
        _updateWriter = updateWriter;
    }

    /// <summary>
    /// Запускает цикл получения обновлений от Telegram.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = _config.AllowedUpdates,
            DropPendingUpdates = true
        };

        try
        {
            await _bot.ReceiveAsync(
                updateHandler: (_, update, ct) => HandleUpdateAsync(update, ct),
                errorHandler: (_, exception, ct) => HandleErrorAsync(exception, ct),
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Игнорируем штатную отмену
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in polling loop");
        }
        finally
        {
            _updateWriter.Complete();
        }
    }

    private async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        // Пишем update в канал (с ожиданием, если канал переполнен)
        await _updateWriter.WriteAsync(update, cancellationToken);
    }

    private Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Polling error");
        return Task.CompletedTask;
    }
}