using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.IntegrationTests.Infrastructure;

/// <summary>
/// Базовый класс интеграционных тестов с мокированным <see cref="IUpdateResponder"/>.
/// Предоставляет хелперы для отправки updates через реальный pipeline
/// и проверки вызовов навигационных методов (AnswerCallbackAsync, ReplyAsync, и т.д.).
///
/// Конкретные подклассы должны быть помечены <c>[Collection(nameof(BotApplicationTests))]</c>.
/// </summary>
public abstract class BotFlowTestBase
{
    protected readonly BotWebApplicationFactory Factory;

    /// <summary>
    /// Мок <see cref="IUpdateResponder"/> — основной способ верификации вызовов в интеграционных тестах.
    /// </summary>
    protected IUpdateResponder MockResponder => Factory.MockResponder;

    protected BotFlowTestBase(BotWebApplicationFactory factory)
    {
        Factory = factory;
    }

    /// <summary>
    /// Отправляет текстовое сообщение через реальный pipeline.
    /// </summary>
    protected async Task SendMessageAsync(
        long userId,
        string text,
        long chatId = 0,
        int messageId = 1)
    {
        chatId = chatId == 0 ? userId : chatId;
        var update = new Update
        {
            Message = new Message
            {
                Id = messageId,
                Text = text,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = chatId, Type = ChatType.Private },
                From = new User { Id = userId, IsBot = false, FirstName = "Test" }
            }
        };
        await ProcessUpdateAsync(update);
    }

    /// <summary>
    /// Отправляет callback query через реальный pipeline.
    /// </summary>
    protected async Task SendCallbackAsync(
        long userId,
        string data,
        long chatId = 0,
        int messageId = 10,
        string callbackQueryId = "cb-1")
    {
        chatId = chatId == 0 ? userId : chatId;
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = callbackQueryId,
                Data = data,
                From = new User { Id = userId, IsBot = false, FirstName = "Test" },
                Message = new Message
                {
                    Id = messageId,
                    Date = DateTime.UtcNow,
                    Chat = new Chat { Id = chatId, Type = ChatType.Private },
                    From = new User { Id = 0, IsBot = true, FirstName = "Bot" }
                }
            }
        };
        await ProcessUpdateAsync(update);
    }

    /// <summary>
    /// Возвращает текущее состояние сессии пользователя.
    /// </summary>
    protected async Task<UserSession> GetSessionAsync(long userId)
    {
        var store = Factory.Services.GetRequiredService<ISessionStore>();
        return await store.GetOrCreateAsync(userId, CancellationToken.None);
    }

    /// <summary>
    /// Сбрасывает записи вызовов на моке UpdateResponder.
    /// Вызывать между действиями для изоляции проверок.
    /// </summary>
    protected void ClearMockCalls() => MockResponder.ClearReceivedCalls();

    /// <summary>
    /// Проверяет, что <see cref="IUpdateResponder.AnswerCallbackAsync"/> был вызван ровно
    /// <paramref name="times"/> раз с любыми аргументами.
    /// </summary>
    protected void VerifyCallbackAnswered(int times = 1, string? text = null)
    {
        MockResponder.Received(times).AnswerCallbackAsync(
            Arg.Any<UpdateContext>(),
            text,
            Arg.Any<bool>());
    }

    /// <summary>
    /// Проверяет, что <see cref="IUpdateResponder.AnswerCallbackAsync"/> не вызывался.
    /// </summary>
    protected void VerifyCallbackNotAnswered()
    {
        MockResponder.DidNotReceive().AnswerCallbackAsync(
            Arg.Any<UpdateContext>(),
            Arg.Any<string>(),
            Arg.Any<bool>());
    }

    private async Task ProcessUpdateAsync(Update update)
    {
        var pipeline = Factory.Services.GetRequiredService<UpdatePipeline>();
        using var scope = Factory.Services.CreateScope();
        var ctx = new UpdateContext(update, scope.ServiceProvider, CancellationToken.None);
        await pipeline.ProcessAsync(ctx);
    }
}