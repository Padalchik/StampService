# TelegramBotFlow v2.0 Foundation Design

**Date:** 2026-04-16
**Status:** Draft
**Scope:** Full framework improvement — architecture, input system, proactive messaging, API safety, wizard/screen improvements, testing, production readiness

## Context

TelegramBotFlow is a .NET 10 framework for building Telegram bots with middleware pipeline, screen-based navigation, wizard FSM, sessions, and ASP.NET Core Minimal API-style route registration.

**Current state:** v1.0.0 — solid foundation with ~700 tests, clean middleware pipeline, typed actions, expression-compiled handlers, wizard FSM with history/rollback.

**Goal:** Make the framework a production-ready foundation for building any type of Telegram bot (funnels, forms, notifications, admin tools) by fixing critical gaps, improving architecture, and expanding primitives.

**Target audience:** Internal team now, open-source later.

**Deployment model:** Single instance with Redis for session persistence. Data must survive restarts.

---

## Design Decisions

### 1. Architecture Refactoring

#### 1.1 Split BotApplication (BREAKING)

**Problem:** `BotApplication` (337 lines) is simultaneously a builder, middleware configurator, route registrar, and runtime launcher. Violates SRP.

**Choice:** Split into three clear responsibilities:

```
BotApplicationBuilder  — service configuration, middleware registration, options
    | Build()
    v
BotApplication         — route registration (MapCommand, MapAction, etc.) + RunAsync
    | RunAsync()
    v
BotRuntime             — lifecycle management (already exists)
```

- `BotApplicationBuilder` — owns `IServiceCollection`, middleware chain, options configuration. `Build()` freezes the pipeline, returns `BotApplication`.
- `BotApplication` — immutable after build. Contains routes, navigation, menu. `RunAsync()` starts runtime.

**Rejected:** Keeping as one class with regions/partial — doesn't solve the coupling, just hides it.

**Migration:** One-time breaking change for v2.0. All `app.Use<T>()` calls move to builder; `app.MapCommand()` stays on app.

#### 1.2 Configurable Parameters

**Problem:** Magic numbers hardcoded throughout the framework:
- Payload LRU cache = 500 (NavigationState)
- Session lock timeout = 10s (InMemorySessionLockProvider)
- Max concurrent updates = 100 (UpdateProcessingWorker)
- Navigation max depth = 20 (UserSession)
- Update channel capacity = 1000 (ServiceCollectionExtensions)

**Choice:** Move all to `BotConfiguration`:

```csharp
public class BotConfiguration
{
    // Existing properties...

    public int PayloadCacheSize { get; set; } = 500;
    public int SessionLockTimeoutSeconds { get; set; } = 10;
    public int MaxConcurrentUpdates { get; set; } = 100;
    public int MaxNavigationDepth { get; set; } = 20;
    public int UpdateChannelCapacity { get; set; } = 1000;
    public int WizardDefaultTtlMinutes { get; set; } = 60;
    public int ShutdownTimeoutSeconds { get; set; } = 30;
    public int TelegramRateLimitPerSecond { get; set; } = 25;
    public int MaxRetryOnRateLimit { get; set; } = 3;
    public string HealthCheckPath { get; set; } = "/health";
}
```

**Rejected:** Separate options classes per subsystem — too granular, BotConfiguration is the single config source, bound from `appsettings.json "Bot"` section.

#### 1.3 Localized Strings — BotMessages (BREAKING minor)

**Problem:** Hardcoded Russian strings: `"← Назад"`, `"☰ Главное меню"`, `"Данные кнопки устарели"`, plus `BotConfiguration.ErrorMessage` is a separate config field for error text.

**Choice:** `BotMessages` class configurable via Options pattern:

```csharp
public class BotMessages
{
    public string BackButton { get; set; } = "← Back";
    public string MenuButton { get; set; } = "☰ Menu";
    public string CloseButton { get; set; } = "← Back";
    public string PayloadExpired { get; set; } = "Button data expired. Please refresh.";
    public string ErrorMessage { get; set; } = "An error occurred. Please try again.";
}
```

- `BotConfiguration.ErrorMessage` removed (moved to `BotMessages.ErrorMessage`).
- Developer overrides via `Configure<BotMessages>(...)`.
- For per-user localization: developer registers custom `BotMessages` provider via DI.

**Rejected:** Resource files (.resx) — too heavy for a framework, and doesn't solve per-user localization. Simple POCO is enough.

#### 1.4 UpdateContext — New Typed Properties

**Problem:** Middleware can't pass typed data down the pipeline. No generic bag (Items/Features) — intentionally rejected to avoid `object` boxing and stringly-typed access.

**Choice:** Add specific typed properties for real, known use cases:

```csharp
public class UpdateContext
{
    // Existing properties...

    // New:
    public IBotUser? User { get; internal set; }       // Set by UserTrackingMiddleware
    public string? HandlerName { get; internal set; }   // Set by UpdateRouter at dispatch
}
```

`UserTrackingMiddleware` sets `User` after lookup/create. `UpdateRouter` sets `HandlerName` when matching a route. Both are `internal set` — only framework code can write them.

**Rejected:** `IDictionary<string, object> Items` — stringly-typed, boxing, no compile-time safety. `IFeatureCollection` — overengineering for 2 properties. If more properties needed in the future — add them explicitly.

---

### 2. Input System Expansion

#### 2.1 Typed Input Properties on UpdateContext

**Problem:** Wizards and PendingInputMiddleware only handle text (`ctx.MessageText`). Real bots accept photos, documents, contacts, locations.

**Choice:** Add properties extracted from `Update.Message` at construction time (same pattern as existing `MessageText`, `CallbackData`):

```csharp
public class UpdateContext
{
    // Existing:
    public string? MessageText { get; }
    public string? CallbackData { get; }

    // New:
    public PhotoSize[]? Photos { get; }
    public Document? Document { get; }
    public Contact? Contact { get; }
    public Location? Location { get; }
    public Voice? Voice { get; }
    public VideoNote? VideoNote { get; }
    public Video? Video { get; }

    public bool HasMedia => Photos != null || Document != null
                         || Voice != null || Video != null || VideoNote != null;
}
```

All extracted in the constructor from `Update.Message`, consistent with existing extraction pattern.

#### 2.2 PendingInputMiddleware — Accept Any Message

**Problem:** Currently checks `MessageText != null` before routing to input handler. Photo/document/contact messages are ignored even when input is pending.

**Choice:** Route any `Message` update when `PendingInputActionId` is set, not just text messages. The handler decides what to do with the input type.

**Condition change:** `context.Update.Message != null && pendingActionId != null` (instead of `context.MessageText != null && pendingActionId != null`).

#### 2.3 Reply Keyboard in ScreenView

**Problem:** `ScreenView` only builds `InlineKeyboardMarkup`. Reply keyboards (contact request, location request, custom buttons at bottom) are not supported in screens.

**Choice:** Add reply keyboard support to `ScreenView`:

```csharp
public ScreenView WithReplyKeyboard(Func<ReplyKeyboard, ReplyKeyboard> configure)
public ScreenView RemoveReplyKeyboard()
```

**Implementation note:** Telegram allows only one `replyMarkup` per message. Navigation message uses `InlineKeyboardMarkup` (edited in place). Reply keyboard requires a separate `SendMessage` call. `ScreenMessageRenderer` must handle this:
- Screen has reply keyboard -> send separate message with `ReplyKeyboardMarkup`
- Screen doesn't have reply keyboard but previous did -> send `ReplyKeyboardRemove`
- Verify behavior during implementation — reply keyboard and inline keyboard cannot coexist in a single message.

#### 2.4 Wizard Steps — Media Input

**Problem:** `TextStep` and `ButtonStep` are convenience wrappers. No equivalents for photo/document/contact input.

**Choice:** No new step types. The base `Step` already accepts `UpdateContext` in its processor. With typed input properties (2.1), the handler can check `ctx.Photos`, `ctx.Document`, etc. directly:

```csharp
builder.Step("avatar",
    render: (ctx, state) => new ScreenView("Send a photo:"),
    process: (ctx, state) =>
    {
        if (ctx.Photos is null)
            return StepResult.Stay("Please send a photo");

        state.PhotoFileId = ctx.Photos[^1].FileId;
        return StepResult.GoTo("next");
    });
```

**Rejected:** Separate `PhotoStep`, `DocumentStep`, `ContactStep` — each would be a thin wrapper that checks one property and calls the handler. Not worth the API surface. Base `Step` is flexible enough.

**Optional convenience helpers:** Can be added later as extension methods (e.g., `PhotoStep`) if the pattern proves common.

---

### 3. Proactive Messaging & Telegram API Safety

#### 3.1 IBotNotifier — Proactive Messaging Primitive

**Problem:** Framework is reactive only — responds to updates. Cannot send messages to users proactively (notifications, reminders, triggered by external events).

**Choice:** `IBotNotifier` — thin abstraction over `ITelegramBotClient` for outgoing messages outside the pipeline:

```csharp
public interface IBotNotifier
{
    Task SendTextAsync(long chatId, string text,
        InlineKeyboardMarkup? keyboard = null,
        ParseMode parseMode = ParseMode.Html,
        CancellationToken ct = default);

    Task SendPhotoAsync(long chatId, InputFile photo,
        string? caption = null,
        InlineKeyboardMarkup? keyboard = null,
        CancellationToken ct = default);

    Task SendDocumentAsync(long chatId, InputFile document,
        string? caption = null,
        CancellationToken ct = default);

    Task CopyMessageAsync(long toChatId, long fromChatId, int messageId,
        CancellationToken ct = default);
}
```

Registered as singleton. Can be injected into any `IHostedService`, background job, external webhook handler. Goes through rate limiter (3.2) transparently via `HttpClient`.

**Not a broadcast engine** — this is a primitive. Bot developer writes their own send logic on top.

#### 3.2 Telegram API Rate Limiter

**Problem:** Telegram limits ~30 msg/sec globally. No protection — mass sending causes 429 Too Many Requests and lost messages.

**Choice:** `TokenBucketRateLimiter` from `System.Threading.RateLimiting` (built-in .NET 7+) applied as `DelegatingHandler` on `HttpClient`:

```csharp
public class TelegramRateLimitHandler : DelegatingHandler
{
    private readonly TokenBucketRateLimiter _limiter;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        using var lease = await _limiter.AcquireAsync(1, ct);

        if (!lease.IsAcquired)
            throw new InvalidOperationException("Rate limit queue full");

        return await base.SendAsync(request, ct);
    }
}
```

**Configuration:** `BotConfiguration.TelegramRateLimitPerSecond = 25` (safety margin from 30 limit).

**.NET mechanism:** `System.Threading.RateLimiting.TokenBucketRateLimiter` — standard library, battle-tested. No custom implementation.

**Rejected:** Custom semaphore-based limiter — reinventing what .NET already provides.

#### 3.3 HTTP Retry Policy

**Problem:** Network timeouts, Telegram 500 errors, 429 retries — all silently fail.

**Choice:** Standard `HttpClientFactory` retry via `Microsoft.Extensions.Http.Resilience` (built on Polly v8):

```csharp
services.AddHttpClient("telegram")
    .AddResilienceHandler("telegram-retry", builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result?.StatusCode is
                    HttpStatusCode.TooManyRequests or
                    HttpStatusCode.InternalServerError or
                    HttpStatusCode.ServiceUnavailable),
            DelayGenerator = args =>
            {
                // Respect Retry-After header for 429
                if (args.Outcome.Result?.Headers.RetryAfter?.Delta is { } delta)
                    return ValueTask.FromResult<TimeSpan?>(delta);
                return ValueTask.FromResult<TimeSpan?>(null); // use default backoff
            }
        });
    });
```

**.NET mechanism:** `Microsoft.Extensions.Http.Resilience` — standard .NET library built on Polly. Handles 429 Retry-After header natively.

**Rejected:** Manual retry loops — error-prone, doesn't handle backoff correctly.

#### 3.4 IBotBroadcaster — Batch Operations

**Problem:** Sending to thousands of users needs: concurrency control, error tracking, blocked user detection, rate limit compliance.

**Choice:** `IBotBroadcaster` — batch sending primitive with result tracking:

```csharp
public interface IBotBroadcaster
{
    Task<BroadcastResult> BroadcastAsync(
        IReadOnlyList<long> chatIds,
        Func<long, BotMessage> messageFactory,
        BroadcastOptions? options = null,
        CancellationToken ct = default);
}

public record BotMessage(
    string Text,
    InlineKeyboardMarkup? Keyboard = null,
    InputFile? Photo = null,
    ParseMode ParseMode = ParseMode.Html);

public class BroadcastOptions
{
    public int MaxConcurrency { get; set; } = 25;
    public Action<long, Exception>? OnError { get; set; }
    public bool MarkBlockedUsers { get; set; } = true;
}

public record BroadcastResult(
    int TotalSent,
    int TotalFailed,
    IReadOnlyList<long> BlockedUserIds,
    IReadOnlyList<long> FailedChatIds);
```

**Implementation:** `Parallel.ForEachAsync` with `MaxConcurrency`. Goes through same `HttpClient` → rate limiter → retry pipeline. Automatically handles:
- **403 Forbidden** -> user blocked bot -> `MarkBlockedAsync` + `BlockedUserIds`
- **429 Too Many Requests** -> retry via resilience handler (3.3)
- **Other errors** -> `FailedChatIds` + `OnError` callback

`messageFactory(chatId)` allows per-user personalization without loading all user data into memory.

**Not a separate outgoing queue** — rate limiter on HttpClient handles throttling. Broadcaster controls concurrency. No additional queue layer needed.

#### 3.5 Chat Member Status Tracking

**Problem:** `AllowedUpdates = [Message, CallbackQuery]`. When user blocks bot — framework doesn't know. `IBotUserStore.MarkBlockedAsync` exists but is never called automatically.

**Choice:**

1. Add `ChatMemberUpdated` to default `AllowedUpdates`
2. Add `MapChatMember` route type in router
3. `UserTrackingMiddleware` auto-calls `MarkBlockedAsync` on `Kicked` status

```csharp
app.MapChatMember((UpdateContext ctx) =>
{
    var status = ctx.Update.MyChatMember!.NewChatMember.Status;
    // ChatMemberStatus.Kicked = blocked, Member = unblocked
});
```

---

### 4. Wizard System Improvements

#### 4.1 InMemoryWizardStore — Fix Memory Leak

**Problem:** Abandoned wizard states stay in `ConcurrentDictionary` forever. `ExpiresAt` only checked on read — unread entries never expire.

**Choice:** Replace `ConcurrentDictionary` with `IMemoryCache`:

```csharp
public class InMemoryWizardStore : IWizardStore
{
    private readonly IMemoryCache _cache;

    public Task SaveAsync(long userId, string wizardId, WizardStorageState state, CancellationToken ct)
    {
        var key = $"{userId}:{wizardId}";
        var options = new MemoryCacheEntryOptions();

        if (state.ExpiresAt.HasValue)
            options.SetAbsoluteExpiration(state.ExpiresAt.Value);
        else
            options.SetSlidingExpiration(TimeSpan.FromMinutes(
                _botConfig.WizardDefaultTtlMinutes)); // default: 60

        _cache.Set(key, state, options);
        return Task.CompletedTask;
    }
}
```

**.NET mechanism:** `IMemoryCache` — standard library, handles eviction automatically via expiration timers and memory pressure. Already used in `UserTrackingMiddleware` — not a new dependency.

**Rejected:** Background cleanup job with `ConcurrentDictionary` — reinvents what `MemoryCache` already does.

#### 4.2 Wizard OnCancelledAsync

**Problem:** When user exits wizard (via `/cancel`, `nav:menu`, empty history on GoBack) — wizard is deleted without cleanup. No way to log, delete temp data, or notify.

**Choice:** Optional virtual method:

```csharp
public abstract class BotWizard<TState>
{
    // Existing:
    public abstract Task<IEndpointResult> OnFinishedAsync(UpdateContext ctx, TState state);

    // New:
    public virtual Task OnCancelledAsync(UpdateContext ctx, TState state)
        => Task.CompletedTask;
}
```

`WizardMiddleware` calls `OnCancelledAsync` before deleting state on cancellation. Default empty implementation — not a breaking change.

---

### 5. Screen & Routing Improvements

#### 5.1 Deep Link Routing

**Problem:** `/start payload` is standard Telegram deep link mechanism. `CommandArgument` is parsed but no convenient routing API.

**Choice:** `MapDeepLink` overload:

```csharp
app.MapDeepLink("start", (UpdateContext ctx, string payload) =>
{
    // payload = "ref_abc123"
    return Task.FromResult(BotResults.NavigateTo<WelcomeScreen>());
});
```

- Registered as HIGH priority route for `/start` when `CommandArgument != null`.
- Regular `MapCommand("start")` remains as NORMAL priority fallback (no payload).
- Router checks deep link route first, then command route.

---

### 6. Pipeline Improvements

#### 6.1 Conditional Middleware (UseWhen)

**Problem:** All middleware receives all updates. No way to have different middleware chains for private vs group chats without full pipeline branching.

**Choice:** `UseWhen` — conditional middleware execution:

```csharp
builder.UseWhen(
    ctx => ctx.Update.Message?.Chat.Type == ChatType.Private,
    pipeline => pipeline
        .Use<SessionMiddleware>()
        .UseWizards()
        .UsePendingInput());
```

**.NET mechanism:** Same pattern as `IApplicationBuilder.UseWhen` in ASP.NET Core. If predicate = false, calls `next` directly (skips the branch).

**Rejected:** Full pipeline branching (endpoint routing style) — overengineering for bot framework.

#### 6.2 Webhook Health Check

**Problem:** No health check endpoint for production deployment (Docker, Kubernetes).

**Choice:** Minimal health endpoint:

```csharp
// In BotRuntime, webhook mode:
app.MapGet(config.HealthCheckPath, () => Results.Ok(new { status = "healthy" }));
```

Checks that the application is alive. Does NOT check Telegram API connectivity (expensive, inappropriate for health probes).

#### 6.3 Graceful Shutdown

**Problem:** `UpdateProcessingWorker` uses `Parallel.ForEachAsync`. On shutdown, in-flight updates may be interrupted.

**Choice:** Use `IHostApplicationLifetime.ApplicationStopping` to drain in-flight updates:

- Stop accepting new updates from channel
- Wait for current in-flight updates to complete (with timeout from `BotConfiguration.ShutdownTimeoutSeconds`)
- Log count of unfinished updates if timeout reached

Standard .NET hosted service graceful shutdown pattern.

#### 6.4 Structured Error Logging

**Problem:** `ErrorHandlingMiddleware` logs exception without context (which handler, which screen, which user).

**Choice:** Use `UpdateContext.HandlerName` (set by router) for structured logging:

```csharp
_logger.LogError(ex,
    "Unhandled exception processing {UpdateType} from user {UserId} "
    + "on screen {Screen}, handler {Handler}",
    context.UpdateType, context.UserId,
    context.Session?.Navigation.CurrentScreen,
    context.HandlerName);
```

---

### 7. Testing & Quality

#### 7.1 Missing Test Coverage

| Component | What to Test | Priority |
|-----------|-------------|----------|
| `ScreenMessageRenderer` | All media transitions (text to photo, photo to video, edit fail to resend) | HIGH |
| `EfBotUserStore` | CRUD, `MarkBlockedAsync`, concurrent access | HIGH |
| `UserTrackingMiddleware` | Auto-create user, update user, cache behavior | HIGH |
| `IBotNotifier` | Send methods, rate limiter integration | HIGH (new code) |
| `IBotBroadcaster` | Concurrency, 403 handling, partial failure, result tracking | HIGH (new code) |
| `WebhookEndpoints` | Secret token validation, invalid requests | MEDIUM |
| `BotApplication` (post-split) | Builder validation, middleware ordering, route registration | MEDIUM |
| Concurrent sessions | Parallel updates from one user, lock timeout | MEDIUM |
| `TelegramRateLimitHandler` | Rate limiting behavior, queue overflow | MEDIUM |
| `UseWhen` | Conditional execution, predicate evaluation | MEDIUM |
| `MapDeepLink` | Priority over MapCommand, payload extraction | MEDIUM |
| `MapChatMember` | Status change routing, auto-block | MEDIUM |

#### 7.2 FakeTelegramBotClient

Test infrastructure for verifying Telegram API calls:

```csharp
public class FakeTelegramBotClient : ITelegramBotClient
{
    public List<SentMessage> SentMessages { get; } = [];
    public List<EditedMessage> EditedMessages { get; } = [];
    public List<int> DeletedMessageIds { get; } = [];
    public List<AnsweredCallback> AnsweredCallbacks { get; } = [];
}
```

Records all outgoing calls for assertion in tests. Replaces the need for complex NSubstitute setups.

---

## Phased Implementation Plan (Summary)

### Phase 1: Architecture (BREAKING)
- 1.1 Split BotApplication
- 1.2 Configurable parameters
- 1.3 BotMessages localization
- 1.4 UpdateContext new properties (User, HandlerName)
- Update all existing tests

### Phase 2: Input System
- 2.1 Typed input properties on UpdateContext
- 2.2 PendingInputMiddleware accepts any message
- 2.3 Reply Keyboard in ScreenView
- Tests for all new functionality

### Phase 3: API Safety
- 3.2 TokenBucketRateLimiter on HttpClient
- 3.3 HTTP retry policy (Microsoft.Extensions.Http.Resilience)
- Tests for rate limiter and retry

### Phase 4: Proactive Messaging
- 3.1 IBotNotifier
- 3.4 IBotBroadcaster
- Tests for notifier and broadcaster

### Phase 5: Wizard & Screen Improvements
- 4.1 MemoryCache for InMemoryWizardStore
- 4.2 OnCancelledAsync
- 5.1 MapDeepLink
- 3.5 ChatMemberUpdated + MapChatMember + auto-block
- Tests

### Phase 6: Pipeline & Production
- 6.1 UseWhen conditional middleware
- 6.2 Health check endpoint
- 6.3 Graceful shutdown
- 6.4 Structured error logging
- Tests

### Phase 7: Test Coverage Gaps
- ScreenMessageRenderer tests
- EfBotUserStore tests
- UserTrackingMiddleware tests
- WebhookEndpoints tests
- Concurrent session stress tests
- FakeTelegramBotClient infrastructure

---

## Rejected Decisions Log

| Proposal | Reason for Rejection |
|----------|---------------------|
| `IDictionary<string, object> Items` on UpdateContext | Stringly-typed, boxing, no compile-time safety |
| `IFeatureCollection` on UpdateContext | Overengineering for 2 properties; still uses `object` internally |
| Outgoing message queue | Duplicates rate limiter + async HttpClient pipeline |
| `BoundedChannelFullMode.DropOldest` | Loses acknowledged updates — data loss |
| Custom semaphore rate limiter | .NET 7+ has `TokenBucketRateLimiter` — use standard library |
| Manual retry loops | `Microsoft.Extensions.Http.Resilience` handles this correctly |
| Route Groups | Telegram has no command namespaces — creates false hierarchy |
| Screen lifecycle hooks | Violates SRP — screens are view layer, data loading belongs in handlers |
| Full pipeline branching | Overengineering — `UseWhen` covers real use cases |
| Separate wizard step types (PhotoStep, DocumentStep) | Base `Step` + typed UpdateContext properties is sufficient and more flexible |
| Background cleanup job for wizard store | `IMemoryCache` handles eviction automatically |
| Wizard state in Redis | Wizard TTL is ~1 hour, state is non-critical — sane to lose on restart |
| Resource files (.resx) for localization | Too heavy for framework, doesn't solve per-user localization |
