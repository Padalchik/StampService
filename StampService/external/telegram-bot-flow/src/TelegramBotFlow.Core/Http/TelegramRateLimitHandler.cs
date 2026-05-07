using System.Threading.RateLimiting;

namespace TelegramBotFlow.Core.Http;

internal sealed class TelegramRateLimitHandler : DelegatingHandler
{
    private readonly TokenBucketRateLimiter _limiter;

    public TelegramRateLimitHandler(TokenBucketRateLimiter limiter)
    {
        _limiter = limiter;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using RateLimitLease lease = await _limiter.AcquireAsync(1, cancellationToken);

        if (!lease.IsAcquired)
            throw new InvalidOperationException("Telegram rate limit queue full. Too many concurrent requests.");

        return await base.SendAsync(request, cancellationToken);
    }
}
