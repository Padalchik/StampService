using TelegramBotFlow.IntegrationTests.Infrastructure;

namespace TelegramBotFlow.IntegrationTests.Routing;

/// <summary>
/// Проверяет инвариант: AnswerCallbackAsync вызывается РОВНО ОДИН РАЗ для каждого callback update.
/// Ответ на callback является ответственностью самого IEndpointResult.
/// </summary>
[Collection(nameof(BotApplicationTests))]
public sealed class CallbackAnsweringTests : BotFlowTestBase
{
    public CallbackAnsweringTests(BotWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task NavBackCallback_AnswersCallbackExactlyOnce()
    {
        long userId = 300_001;
        await SendMessageAsync(userId, "/start");
        await SendCallbackAsync(userId, "nav:profile");
        ClearMockCalls();

        await SendCallbackAsync(userId, "nav:back");

        VerifyCallbackAnswered(times: 1);
    }

    [Fact]
    public async Task NavMenuCallback_AnswersCallbackExactlyOnce()
    {
        long userId = 300_002;
        await SendMessageAsync(userId, "/start");
        await SendCallbackAsync(userId, "nav:profile");
        ClearMockCalls();

        await SendCallbackAsync(userId, "nav:menu");

        VerifyCallbackAnswered(times: 1);
    }

    [Fact]
    public async Task NavCloseCallback_AnswersCallbackExactlyOnce()
    {
        long userId = 300_003;
        await SendMessageAsync(userId, "/start");
        ClearMockCalls();

        await SendCallbackAsync(userId, "nav:close");

        VerifyCallbackAnswered(times: 1);
    }

    [Fact]
    public async Task NavToScreenCallback_AnswersCallbackExactlyOnce()
    {
        long userId = 300_004;
        await SendMessageAsync(userId, "/start");
        ClearMockCalls();

        await SendCallbackAsync(userId, "nav:profile");

        VerifyCallbackAnswered(times: 1);
    }

    [Fact]
    public async Task TextMessage_DoesNotAnswerCallbackQuery()
    {
        long userId = 300_005;
        ClearMockCalls();

        await SendMessageAsync(userId, "/start");

        VerifyCallbackNotAnswered();
    }
}