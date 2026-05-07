using System.Net;
using System.Threading.RateLimiting;
using FluentAssertions;
using TelegramBotFlow.Core.Http;

namespace TelegramBotFlow.Core.Tests.Http;

public sealed class TelegramRateLimitHandlerTests
{
    [Fact]
    public async Task Request_WithAvailableToken_Succeeds()
    {
        var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 10,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = 10,
            QueueLimit = 0
        });

        var handler = new TelegramRateLimitHandler(limiter)
        {
            InnerHandler = new FakeHttpHandler(HttpStatusCode.OK)
        };

        using var client = new HttpClient(handler);
        var response = await client.GetAsync("https://api.telegram.org/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_WhenRateLimited_Queues()
    {
        var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 1,
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(100),
            TokensPerPeriod = 1,
            QueueLimit = 5
        });

        var handler = new TelegramRateLimitHandler(limiter)
        {
            InnerHandler = new FakeHttpHandler(HttpStatusCode.OK)
        };

        using var client = new HttpClient(handler);
        var r1 = await client.GetAsync("https://api.telegram.org/test1");
        r1.StatusCode.Should().Be(HttpStatusCode.OK);

        var r2 = await client.GetAsync("https://api.telegram.org/test2");
        r2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class FakeHttpHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
