# Phase 3: API Safety — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Telegram API rate limiting and HTTP retry policy to prevent 429 errors and handle transient failures.

**Architecture:** TokenBucketRateLimiter (System.Threading.RateLimiting) as DelegatingHandler on the Telegram HttpClient. Microsoft.Extensions.Http.Resilience for retry with exponential backoff and Retry-After header support. Both are transparent — all Telegram API calls go through them automatically.

**Tech Stack:** .NET 10, System.Threading.RateLimiting, Microsoft.Extensions.Http.Resilience (Polly v8), xUnit

**Spec:** `docs/specs/2026-04-16-v2-foundation-design.md` sections 3.2–3.3

**Depends on:** Phase 1 completed (BotConfiguration has TelegramRateLimitPerSecond, MaxRetryOnRateLimit)

---

## File Map

**New files:**
- `src/TelegramBotFlow.Core/Http/TelegramRateLimitHandler.cs` — DelegatingHandler with TokenBucketRateLimiter
- `tests/TelegramBotFlow.Core.Tests/Http/TelegramRateLimitHandlerTests.cs`

**Modified files:**
- `src/TelegramBotFlow.Core/Extensions/ServiceCollectionExtensions.cs` — register HttpClient with handlers
- `src/TelegramBotFlow.Core/TelegramBotFlow.Core.csproj` — add Microsoft.Extensions.Http.Resilience package
- `Directory.Packages.props` — add Microsoft.Extensions.Http.Resilience version

---

### Task 1: Add NuGet dependency

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/TelegramBotFlow.Core/TelegramBotFlow.Core.csproj`

- [ ] **Step 1: Add package reference**

In `Directory.Packages.props`, add to the `<ItemGroup>`:

```xml
<PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="10.0.0" />
```

In `TelegramBotFlow.Core.csproj`, add:

```xml
<PackageReference Include="Microsoft.Extensions.Http.Resilience" />
```

- [ ] **Step 2: Restore and build**

Run: `dotnet restore TelegramBotFlow.slnx && dotnet build TelegramBotFlow.slnx`
Expected: Compiles

- [ ] **Step 3: Commit**

```bash
git add Directory.Packages.props src/TelegramBotFlow.Core/TelegramBotFlow.Core.csproj
git commit -m "deps: add Microsoft.Extensions.Http.Resilience for Telegram API retry"
```

---

### Task 2: Implement TelegramRateLimitHandler

**Files:**
- Create: `src/TelegramBotFlow.Core/Http/TelegramRateLimitHandler.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Http/TelegramRateLimitHandlerTests.cs`

- [ ] **Step 1: Write the tests**

```csharp
// tests/TelegramBotFlow.Core.Tests/Http/TelegramRateLimitHandlerTests.cs
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

        // First request consumes the token
        var r1 = await client.GetAsync("https://api.telegram.org/test1");
        r1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second request queued, completes after replenishment
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
```

- [ ] **Step 2: Run tests — expect failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~TelegramRateLimitHandlerTests"`
Expected: FAIL — class not found

- [ ] **Step 3: Implement TelegramRateLimitHandler**

```csharp
// src/TelegramBotFlow.Core/Http/TelegramRateLimitHandler.cs
using System.Threading.RateLimiting;

namespace TelegramBotFlow.Core.Http;

/// <summary>
/// DelegatingHandler that applies token bucket rate limiting to all outgoing Telegram API requests.
/// Uses System.Threading.RateLimiting.TokenBucketRateLimiter.
/// </summary>
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
```

- [ ] **Step 4: Run tests — expect pass**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~TelegramRateLimitHandlerTests"`
Expected: All pass

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add TelegramRateLimitHandler with TokenBucketRateLimiter"
```

---

### Task 3: Wire rate limiter and retry to HttpClient in DI

**Files:**
- Modify: `src/TelegramBotFlow.Core/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Update ITelegramBotClient registration**

Replace the direct `TelegramBotClient` singleton registration with `HttpClientFactory`-based registration that includes rate limiting and resilience:

```csharp
// In AddTelegramBotFlow(), replace the ITelegramBotClient registration with:

var rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
{
    TokenLimit = config.TelegramRateLimitPerSecond,
    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
    TokensPerPeriod = config.TelegramRateLimitPerSecond,
    QueueLimit = config.TelegramRateLimitPerSecond * 10 // queue up to 10 seconds of requests
});

services.AddHttpClient("telegram")
    .AddHttpMessageHandler(() => new TelegramRateLimitHandler(rateLimiter))
    .AddResilienceHandler("telegram-retry", builder =>
    {
        builder.AddRetry(new Microsoft.Extensions.Http.Resilience.HttpRetryStrategyOptions
        {
            MaxRetryAttempts = config.MaxRetryOnRateLimit,
            BackoffType = Polly.DelayBackoffType.Exponential,
            ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result?.StatusCode is
                    System.Net.HttpStatusCode.TooManyRequests or
                    System.Net.HttpStatusCode.InternalServerError or
                    System.Net.HttpStatusCode.ServiceUnavailable),
            DelayGenerator = args =>
            {
                if (args.Outcome.Result?.Headers.RetryAfter?.Delta is { } delta)
                    return ValueTask.FromResult<TimeSpan?>(delta);
                return ValueTask.FromResult<TimeSpan?>(null);
            }
        });
    });

services.AddSingleton<ITelegramBotClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("telegram");
    return new TelegramBotClient(config.Token, httpClient);
});
```

- [ ] **Step 2: Build and test**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: wire TokenBucketRateLimiter and resilience retry to Telegram HttpClient"
```

---

### Task 4: Final verification

- [ ] **Step 1: Full test suite**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass, 0 warnings
