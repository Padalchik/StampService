# Phase 5: Wizard & Screen Improvements — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix InMemoryWizardStore memory leak, add wizard OnCancelledAsync, add MapDeepLink routing, add ChatMemberUpdated tracking with auto-block.

**Architecture:** InMemoryWizardStore uses IMemoryCache instead of ConcurrentDictionary. BotWizard gains virtual OnCancelledAsync. Router gains MapDeepLink (HIGH priority) and MapChatMember route types. UserTrackingMiddleware auto-calls MarkBlockedAsync on Kicked status.

**Tech Stack:** .NET 10, Microsoft.Extensions.Caching.Memory, Telegram.Bot 22.9.0, xUnit, FluentAssertions, NSubstitute

**Spec:** `docs/specs/2026-04-16-v2-foundation-design.md` sections 3.5, 4.1–4.2, 5.1

**Depends on:** Phase 1 completed

---

## File Map

**Modified files:**
- `src/TelegramBotFlow.Core/Wizards/InMemoryWizardStore.cs` — replace ConcurrentDictionary with IMemoryCache
- `src/TelegramBotFlow.Core/Wizards/BotWizard.cs` — add virtual OnCancelledAsync
- `src/TelegramBotFlow.Core/Pipeline/Middlewares/WizardMiddleware.cs` — call OnCancelledAsync on cancel
- `src/TelegramBotFlow.Core/Hosting/BotApplication.cs` — add MapDeepLink, MapChatMember
- `src/TelegramBotFlow.Core/Routing/RouteEntry.cs` — add DEEP_LINK and CHAT_MEMBER route types
- `src/TelegramBotFlow.Core/Routing/UpdateRouter.cs` — handle new route types
- `src/TelegramBotFlow.Core/Hosting/BotConfiguration.cs` — add ChatMemberUpdated to default AllowedUpdates
- `src/TelegramBotFlow.Core/Pipeline/Middlewares/UserTrackingMiddleware.cs` — auto-mark blocked on MyChatMember

**Test files:**
- `tests/TelegramBotFlow.Core.Tests/Wizards/InMemoryWizardStoreTests.cs` — new/modify
- `tests/TelegramBotFlow.Core.Tests/Wizards/WizardCancellationTests.cs` — new
- `tests/TelegramBotFlow.Core.Tests/Routing/DeepLinkRoutingTests.cs` — new
- `tests/TelegramBotFlow.Core.Tests/Routing/ChatMemberRoutingTests.cs` — new

---

### Task 1: Fix InMemoryWizardStore memory leak

**Files:**
- Modify: `src/TelegramBotFlow.Core/Wizards/InMemoryWizardStore.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Wizards/InMemoryWizardStoreTests.cs`

- [ ] **Step 1: Write tests**

```csharp
// tests/TelegramBotFlow.Core.Tests/Wizards/InMemoryWizardStoreTests.cs
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Tests.Wizards;

public sealed class InMemoryWizardStoreTests
{
    private readonly InMemoryWizardStore _store;

    public InMemoryWizardStoreTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var config = Options.Create(new BotConfiguration { WizardDefaultTtlMinutes = 60 });
        _store = new InMemoryWizardStore(cache, config);
    }

    [Fact]
    public async Task SaveAndGet_ReturnsState()
    {
        var state = new WizardStorageState { CurrentStepId = "step1", PayloadJson = "{}" };

        await _store.SaveAsync(1, "wiz1", state);
        var result = await _store.GetAsync(1, "wiz1");

        result.Should().NotBeNull();
        result!.CurrentStepId.Should().Be("step1");
    }

    [Fact]
    public async Task Delete_RemovesState()
    {
        var state = new WizardStorageState { CurrentStepId = "step1", PayloadJson = "{}" };
        await _store.SaveAsync(1, "wiz1", state);

        await _store.DeleteAsync(1, "wiz1");
        var result = await _store.GetAsync(1, "wiz1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Get_NonExistent_ReturnsNull()
    {
        var result = await _store.GetAsync(999, "nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExpiredState_ReturnsNull()
    {
        var state = new WizardStorageState
        {
            CurrentStepId = "step1",
            PayloadJson = "{}",
            ExpiresAt = DateTime.UtcNow.AddMilliseconds(-1) // already expired
        };

        await _store.SaveAsync(1, "wiz1", state);
        var result = await _store.GetAsync(1, "wiz1");

        result.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run tests — expect failure (constructor changed)**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~InMemoryWizardStoreTests"`

- [ ] **Step 3: Rewrite InMemoryWizardStore**

```csharp
// src/TelegramBotFlow.Core/Wizards/InMemoryWizardStore.cs
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Wizards;

internal sealed class InMemoryWizardStore : IWizardStore
{
    private readonly IMemoryCache _cache;
    private readonly int _defaultTtlMinutes;

    public InMemoryWizardStore(IMemoryCache cache, IOptions<BotConfiguration> config)
    {
        _cache = cache;
        _defaultTtlMinutes = config.Value.WizardDefaultTtlMinutes;
    }

    public Task<WizardStorageState?> GetAsync(long userId, string wizardId, CancellationToken ct = default)
    {
        var key = FormatKey(userId, wizardId);
        _cache.TryGetValue(key, out WizardStorageState? state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(long userId, string wizardId, WizardStorageState state, CancellationToken ct = default)
    {
        var key = FormatKey(userId, wizardId);
        var options = new MemoryCacheEntryOptions();

        if (state.ExpiresAt.HasValue)
            options.SetAbsoluteExpiration(state.ExpiresAt.Value);
        else
            options.SetSlidingExpiration(TimeSpan.FromMinutes(_defaultTtlMinutes));

        _cache.Set(key, state, options);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(long userId, string wizardId, CancellationToken ct = default)
    {
        _cache.Remove(FormatKey(userId, wizardId));
        return Task.CompletedTask;
    }

    private static string FormatKey(long userId, string wizardId) => $"wizard:{userId}:{wizardId}";
}
```

- [ ] **Step 4: Update DI registration if needed**

In `ServiceCollectionExtensions` or `AddWizards`, ensure `InMemoryWizardStore` is registered with `IMemoryCache` available. `IMemoryCache` should already be registered (used by UserTrackingMiddleware). If not, add `services.AddMemoryCache()`.

- [ ] **Step 5: Run tests — expect pass**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~InMemoryWizardStoreTests"`
Expected: All pass

- [ ] **Step 6: Run full suite**

Run: `dotnet test TelegramBotFlow.slnx`
Expected: All pass (existing wizard tests still work)

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "fix: replace ConcurrentDictionary with IMemoryCache in InMemoryWizardStore to prevent memory leaks"
```

---

### Task 2: Add OnCancelledAsync to BotWizard

**Files:**
- Modify: `src/TelegramBotFlow.Core/Wizards/BotWizard.cs`
- Modify: `src/TelegramBotFlow.Core/Pipeline/Middlewares/WizardMiddleware.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Wizards/WizardCancellationTests.cs`

- [ ] **Step 1: Write the test**

```csharp
// tests/TelegramBotFlow.Core.Tests/Wizards/WizardCancellationTests.cs
using FluentAssertions;
using NSubstitute;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Tests.Wizards;

public sealed class WizardCancellationTests
{
    [Fact]
    public void OnCancelledAsync_DefaultImplementation_CompletesSuccessfully()
    {
        var wizard = new TestCancellableWizard();

        Func<Task> act = () => wizard.TestOnCancelled(
            TestHelpers.CreateMessageContext("/cancel"),
            new TestState());

        act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnCancelledAsync_CustomImplementation_IsCalled()
    {
        var wizard = new TrackingCancellableWizard();
        var state = new TestState();

        await wizard.TestOnCancelled(
            TestHelpers.CreateMessageContext("/cancel"),
            state);

        wizard.WasCancelled.Should().BeTrue();
    }

    private class TestState { public string? Name { get; set; } }

    private class TestCancellableWizard : BotWizard<TestState>
    {
        protected override void ConfigureSteps(WizardBuilder<TestState> builder) { }
        public override Task<IEndpointResult> OnFinishedAsync(UpdateContext ctx, TestState state)
            => Task.FromResult(BotResults.Back());

        // Expose for testing:
        public Task TestOnCancelled(UpdateContext ctx, TestState state) => OnCancelledAsync(ctx, state);
    }

    private class TrackingCancellableWizard : BotWizard<TestState>
    {
        public bool WasCancelled { get; private set; }

        protected override void ConfigureSteps(WizardBuilder<TestState> builder) { }
        public override Task<IEndpointResult> OnFinishedAsync(UpdateContext ctx, TestState state)
            => Task.FromResult(BotResults.Back());

        protected override Task OnCancelledAsync(UpdateContext ctx, TestState state)
        {
            WasCancelled = true;
            return Task.CompletedTask;
        }

        public Task TestOnCancelled(UpdateContext ctx, TestState state) => OnCancelledAsync(ctx, state);
    }
}
```

- [ ] **Step 2: Run test — expect failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~WizardCancellationTests"`
Expected: FAIL — `OnCancelledAsync` not found

- [ ] **Step 3: Add OnCancelledAsync to BotWizard**

In `src/TelegramBotFlow.Core/Wizards/BotWizard.cs`, add:

```csharp
/// <summary>
/// Called when the wizard is cancelled by the user (/cancel, nav:menu, empty back history).
/// Override to perform cleanup. Default implementation does nothing.
/// </summary>
protected virtual Task OnCancelledAsync(UpdateContext context, TState state)
    => Task.CompletedTask;
```

Also add to `IBotWizard` interface a type-erased version that `WizardMiddleware` can call:

```csharp
// In IBotWizard:
Task OnCancelledAsync(UpdateContext context, WizardStorageState state);
```

Implement in `BotWizard<TState>`: deserialize state, call the typed `OnCancelledAsync`.

- [ ] **Step 4: Update WizardMiddleware to call OnCancelledAsync**

In `WizardMiddleware.cs`, before `_wizardStore.DeleteAsync()` on cancellation paths (lines 44-45, 54-55), add:

```csharp
// Resolve wizard instance and call OnCancelledAsync before deleting state
if (_wizardRegistry.HasWizard(activeWizardId))
{
    var storageState = await _wizardStore.GetAsync(context.UserId, activeWizardId, context.CancellationToken);
    if (storageState != null)
    {
        var wizard = _wizardRegistry.Resolve(activeWizardId, context.RequestServices);
        await wizard.OnCancelledAsync(context, storageState);
    }
}
```

- [ ] **Step 5: Run tests — expect pass**

Run: `dotnet test TelegramBotFlow.slnx`
Expected: All pass

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add OnCancelledAsync virtual method to BotWizard for cleanup on cancellation"
```

---

### Task 3: Add MapDeepLink routing

**Files:**
- Modify: `src/TelegramBotFlow.Core/Hosting/BotApplication.cs`
- Modify: `src/TelegramBotFlow.Core/Routing/RouteEntry.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Routing/DeepLinkRoutingTests.cs`

- [ ] **Step 1: Write the test**

```csharp
// tests/TelegramBotFlow.Core.Tests/Routing/DeepLinkRoutingTests.cs
using FluentAssertions;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.Core.Tests.Routing;

public sealed class DeepLinkRoutingTests
{
    [Fact]
    public void DeepLinkRoute_MatchesStartWithPayload()
    {
        var route = RouteEntry.DeepLink("start",
            _ => Task.CompletedTask);

        var ctx = TestHelpers.CreateMessageContext("/start ref_abc123");

        route.Matches(ctx).Should().BeTrue();
    }

    [Fact]
    public void DeepLinkRoute_DoesNotMatchStartWithoutPayload()
    {
        var route = RouteEntry.DeepLink("start",
            _ => Task.CompletedTask);

        var ctx = TestHelpers.CreateMessageContext("/start");

        route.Matches(ctx).Should().BeFalse();
    }

    [Fact]
    public void DeepLinkRoute_HasHighPriority()
    {
        var route = RouteEntry.DeepLink("start",
            _ => Task.CompletedTask);

        route.Priority.Should().Be(RoutePriority.HIGH);
    }
}
```

- [ ] **Step 2: Run tests — expect failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~DeepLinkRoutingTests"`

- [ ] **Step 3: Add DeepLink factory to RouteEntry**

In `RouteEntry.cs`, add a factory method:

```csharp
public static RouteEntry DeepLink(string command, UpdateDelegate handler) =>
    new(RouteType.COMMAND, handler,
        pattern: $"/{command}",
        predicate: ctx => ctx.CommandArgument != null,
        priority: RoutePriority.HIGH);
```

- [ ] **Step 4: Add MapDeepLink to BotApplication**

In `BotApplication.cs`:

```csharp
/// <summary>
/// Maps a deep link handler for /command with payload. Higher priority than MapCommand.
/// The payload is available via ctx.CommandArgument.
/// </summary>
public BotApplication MapDeepLink(string command, Delegate handler)
{
    var route = RouteEntry.DeepLink(command, HandlerDelegateFactory.Create(handler));
    _router.AddRoute(route);
    return this;
}
```

- [ ] **Step 5: Run tests — expect pass**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~DeepLinkRoutingTests"`
Expected: All pass

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add MapDeepLink for deep link routing with HIGH priority"
```

---

### Task 4: Add ChatMemberUpdated tracking

**Files:**
- Modify: `src/TelegramBotFlow.Core/Hosting/BotConfiguration.cs` — update default AllowedUpdates
- Modify: `src/TelegramBotFlow.Core/Routing/RouteEntry.cs` — add CHAT_MEMBER route type
- Modify: `src/TelegramBotFlow.Core/Hosting/BotApplication.cs` — add MapChatMember
- Modify: `src/TelegramBotFlow.Core/Pipeline/Middlewares/UserTrackingMiddleware.cs` — auto-block on Kicked
- Create: `tests/TelegramBotFlow.Core.Tests/Routing/ChatMemberRoutingTests.cs`

- [ ] **Step 1: Write tests**

```csharp
// tests/TelegramBotFlow.Core.Tests/Routing/ChatMemberRoutingTests.cs
using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.Core.Tests.Routing;

public sealed class ChatMemberRoutingTests
{
    [Fact]
    public void ChatMemberRoute_MatchesMyChatMemberUpdate()
    {
        var route = RouteEntry.ChatMember(_ => Task.CompletedTask);

        var update = new Update
        {
            MyChatMember = new ChatMemberUpdated
            {
                Chat = new Chat { Id = 123, Type = ChatType.Private },
                From = new User { Id = 456, FirstName = "Test" },
                Date = DateTime.UtcNow,
                OldChatMember = new ChatMemberMember { User = new User { Id = 456, FirstName = "Test" } },
                NewChatMember = new ChatMemberBanned { User = new User { Id = 456, FirstName = "Test" }, UntilDate = null }
            }
        };
        var ctx = new UpdateContext(update, Substitute.For<IServiceProvider>());

        route.Matches(ctx).Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests — expect failure**

- [ ] **Step 3: Implement ChatMember route type and MapChatMember**

Add `CHAT_MEMBER` to `RouteType` enum in `RouteEntry.cs`.

Add factory:
```csharp
public static RouteEntry ChatMember(UpdateDelegate handler) =>
    new(RouteType.CHAT_MEMBER, handler,
        pattern: null,
        predicate: ctx => ctx.Update.MyChatMember != null,
        priority: RoutePriority.NORMAL);
```

Add to `BotApplication.cs`:
```csharp
public BotApplication MapChatMember(Delegate handler)
{
    var route = RouteEntry.ChatMember(HandlerDelegateFactory.Create(handler));
    _router.AddRoute(route);
    return this;
}
```

- [ ] **Step 4: Update default AllowedUpdates**

In `BotConfiguration.cs`, change default:
```csharp
public UpdateType[] AllowedUpdates { get; set; } =
    [UpdateType.Message, UpdateType.CallbackQuery, UpdateType.MyChatMember];
```

- [ ] **Step 5: Add auto-block in UserTrackingMiddleware**

In `UserTrackingMiddleware.InvokeAsync`, add at the beginning:
```csharp
if (context.Update.MyChatMember is { NewChatMember.Status: ChatMemberStatus.Kicked } chatMember)
{
    await _userStore.MarkBlockedAsync(chatMember.From.Id, context.CancellationToken);
    return; // Don't process further
}
```

- [ ] **Step 6: Run all tests**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add ChatMemberUpdated tracking with auto-block and MapChatMember routing"
```
