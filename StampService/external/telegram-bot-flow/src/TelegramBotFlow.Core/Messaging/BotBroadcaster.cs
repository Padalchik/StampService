using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace TelegramBotFlow.Core.Messaging;

internal sealed class BotBroadcaster : IBotBroadcaster
{
    private readonly ITelegramBotClient _bot;

    public BotBroadcaster(ITelegramBotClient bot)
    {
        _bot = bot;
    }

    public async Task<BroadcastResult> BroadcastAsync(
        IReadOnlyList<long> chatIds,
        Func<long, BotMessage> messageFactory,
        BroadcastOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new BroadcastOptions();

        int sent = 0;
        int failed = 0;
        ConcurrentBag<long> blockedUsers = [];
        ConcurrentBag<long> failedChats = [];

        await Parallel.ForEachAsync(chatIds, new ParallelOptions
        {
            MaxDegreeOfParallelism = options.MaxConcurrency,
            CancellationToken = ct
        }, async (chatId, token) =>
        {
            try
            {
                BotMessage message = messageFactory(chatId);

                if (message.Photo is not null)
                {
                    await _bot.SendPhoto(
                        chatId,
                        message.Photo,
                        caption: message.Text,
                        replyMarkup: message.Keyboard,
                        parseMode: message.ParseMode,
                        cancellationToken: token);
                }
                else
                {
                    await _bot.SendMessage(
                        chatId,
                        message.Text,
                        replyMarkup: message.Keyboard,
                        parseMode: message.ParseMode,
                        cancellationToken: token);
                }

                Interlocked.Increment(ref sent);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 403 && options.MarkBlockedUsers)
            {
                blockedUsers.Add(chatId);
                Interlocked.Increment(ref failed);
                options.OnError?.Invoke(chatId, ex);
            }
            catch (Exception ex)
            {
                failedChats.Add(chatId);
                Interlocked.Increment(ref failed);
                options.OnError?.Invoke(chatId, ex);
            }
        });

        return new BroadcastResult(
            sent,
            failed,
            [.. blockedUsers],
            [.. failedChats]);
    }
}
